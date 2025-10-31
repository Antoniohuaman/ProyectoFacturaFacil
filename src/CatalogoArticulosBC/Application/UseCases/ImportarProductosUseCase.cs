using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Application.Specifications;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases
{
    public sealed class ImportarProductosUseCase
    {
        public enum Mode { CreateOnly, Upsert, UpdateOnly }
        public enum OnSkuConflict { Skip, Update, Error }

        public readonly record struct Request(
            Stream FileStream,
            string FileName,
            Mode OperationMode = Mode.Upsert,
            OnSkuConflict OnConflict = OnSkuConflict.Update,
            int? BatchSize = null,
            bool ValidateOnly = false
        );

        public readonly record struct RowError(int Fila, string? Columna, string Codigo, string Mensaje);

        public readonly record struct Summary(
            int TotalFilas,
            int Procesadas,
            int Creadas,
            int Actualizadas,
            int Omitidas,
            int ConError,
            long DuracionMs
        );

        public readonly record struct Response(Summary Resumen, IReadOnlyList<RowError> Errores);

        private readonly IImportExportService _parser;
        private readonly IImportSchemaProvider _schemaProvider;
        private readonly IProductoRepository _productos;
    private readonly CatalogoArticulosBC.Application.Interfaces.IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public ImportarProductosUseCase(
            IImportExportService parser,
            IImportSchemaProvider schemaProvider,
            IProductoRepository productos,
            CatalogoArticulosBC.Application.Interfaces.IUnitOfWork uow,
            ITenantContext tenant)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
            _productos = productos ?? throw new ArgumentNullException(nameof(productos));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<Response> Handle(Request req, CancellationToken ct = default)
        {
            if (req.FileStream is null) throw new ArgumentNullException(nameof(req.FileStream));

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var parsed = await _parser.ParseAsync(req.FileStream, req.FileName, ct);
            var detectedHeaders = parsed.Headers.Select(h => h?.Trim() ?? string.Empty).ToList();

            // Validar mínimos presentes
            var minima = _schemaProvider.GetMinimumRequiredHeaders();
            var missing = minima.Where(m => !detectedHeaders.Any(d => string.Equals(d, m, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (missing.Any())
                throw new BusinessRuleException($"Archivo inválido: faltan columnas mínimas: {string.Join(',', missing)}");

            var rows = parsed.Rows.ToList();
            int total = rows.Count;

            int procesadas = 0, creadas = 0, actualizadas = 0, omitidas = 0, conError = 0;
            var errores = new List<RowError>();

            int batchSize = req.BatchSize ?? 100;
            var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            var toCommit = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                var filaIndex = i + 2; // header row = 1
                var row = rows[i];

                try
                {
                    // Construir DTO de importación usando nombres de cabecera del proveedor (case-insensitive)
                    string Get(string header) => GetCellValue(row, detectedHeaders, header);

                    var dto = new ProductoImportacionDto
                    {
                        Sku = Get("Sku"),
                        Nombre = Get("Nombre"),
                        UnidadMedida = Get("UnidadMedida"),
                        AfectacionImpuesto = Get("AfectacionImpuesto"),
                        Categoria = Get("Categoria"),
                        AlmacenesAsignados = ParseEstablecimientos(Get("AlmacenesAsignados")),
                        Descripcion = Get("Descripcion"),
                        Marca = Get("Marca"),
                        Modelo = Get("Modelo"),
                        CodigoBarras = Get("CodigoBarras"),
                        CodigoFabrica = Get("CodigoFabrica"),
                        CodigoSUNAT = Get("CodigoSUNAT"),
                        PrecioVenta = ParseDecimal(Get("PrecioVenta")),
                        Peso = ParseDecimal(Get("Peso")),
                        Descuento = ParseDecimal(Get("Descuento")),
                        Moneda = Get("Moneda"),
                        TipoExistencia = Get("TipoExistencia")
                    };

                    // Validación básica por Specification de importación
                    var spec = new ProductoImportacionSpecification();
                    var specRes = spec.IsSatisfiedBy(dto);
                    if (!specRes.IsSatisfied)
                    {
                        conError++;
                        errores.Add(new RowError(filaIndex, specRes.Field, specRes.ErrorCode ?? "SPEC", specRes.Message ?? "Error de validación"));
                        continue;
                    }

                    // Construir VOs
                    var sku = Sku.Crear(dto.Sku);
                    var nombre = new NombreProducto(dto.Nombre!);
                    var unidad = UnidadDeMedida.From(dto.UnidadMedida!);
                    var afectacion = AfectacionImpuesto.From(dto.AfectacionImpuesto!);
                    var categoria = new Categoria(dto.Categoria!);
                    var moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? Moneda.Create("PEN") : Moneda.Create(dto.Moneda);
                    TasaImpuesto tasa = afectacion.GravaImpuesto ? TasaObligatoria(dto) : TasaImpuesto.Cero;

                    var establecimientos = dto.AlmacenesAsignados?.Select(s => EstablecimientoId.FromString(s)).ToList()
                                           ?? throw new ArgumentException("Al menos un establecimiento asignado es obligatorio.");

                    // Decide acción según modo y conflicto
                    var exists = await _productos.ExisteSkuAsync(sku, empresaId, ct);

                    if (req.OperationMode == Mode.CreateOnly)
                    {
                        if (exists)
                        {
                            // conflicto
                            switch (req.OnConflict)
                            {
                                case OnSkuConflict.Skip:
                                    omitidas++; break;
                                case OnSkuConflict.Update:
                                    // Obtener y actualizar
                                    var toUpdate = await _productos.GetBySkuAsync(sku, empresaId);
                                    if (toUpdate is null) { omitidas++; break; }
                                    toUpdate.EditarDatos(
                                        nombre: nombre,
                                        unidadMedida: unidad,
                                        afectacionImpuesto: afectacion,
                                        tasaImpuesto: tasa,
                                        categoria: categoria,
                                        marca: null,
                                        precioVenta: null,
                                        centroDeCosto: null,
                                        peso: dto.Peso.HasValue ? new Peso(dto.Peso.Value) : null,
                                        codigoBarras: string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : new CodigoBarras(dto.CodigoBarras),
                                        codigoFabrica: string.IsNullOrWhiteSpace(dto.CodigoFabrica) ? null : new CodigoFabrica(dto.CodigoFabrica),
                                        tipo: TipoProducto.Bien,
                                        codigoSunat: string.IsNullOrWhiteSpace(dto.CodigoSUNAT) ? null : new CodigoSUNAT(dto.CodigoSUNAT),
                                        establecimientosAsignados: establecimientos,
                                        asignarATodosLosEstablecimientos: false,
                                        imagenPrincipalId: null,
                                        descripcion: dto.Descripcion,
                                        tipoExistencia: TipoExistencia.ProductosTerminados
                                    );
                                    await _productos.UpdateAsync(toUpdate);
                                    actualizadas++; break;
                                case OnSkuConflict.Error:
                                    conError++; errores.Add(new RowError(filaIndex, "Sku", "SKU_CONFLICT", "SKU ya existe")); break;
                            }
                        }
                        else
                        {
                            // Crear
                            var producto = new ProductoSimple(
                                empresaId: empresaId,
                                moneda: moneda,
                                sku: sku,
                                nombre: nombre,
                                unidadMedida: unidad,
                                afectacionImpuesto: afectacion,
                                tasaImpuesto: tasa,
                                categoria: categoria,
                                establecimientosAsignados: establecimientos,
                                descripcion: dto.Descripcion,
                                marca: null,
                                precioVenta: null,
                                codigoSunat: string.IsNullOrWhiteSpace(dto.CodigoSUNAT) ? null : new CodigoSUNAT(dto.CodigoSUNAT),
                                centroDeCosto: null,
                                peso: dto.Peso.HasValue ? new Peso(dto.Peso.Value) : null,
                                codigoBarras: string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : new CodigoBarras(dto.CodigoBarras),
                                codigoFabrica: string.IsNullOrWhiteSpace(dto.CodigoFabrica) ? null : new CodigoFabrica(dto.CodigoFabrica),
                                tipo: TipoProducto.Bien,
                                tipoExistencia: TipoExistencia.ProductosTerminados,
                                asignarATodosLosEstablecimientos: false,
                                imagenPrincipalId: null
                            );
                            await _productos.AddAsync(producto, ct);
                            creadas++;
                        }
                    }
                    else if (req.OperationMode == Mode.Upsert)
                    {
                        if (exists)
                        {
                            var prod = await _productos.GetBySkuAsync(sku, empresaId);
                            if (prod is null)
                            {
                                conError++; errores.Add(new RowError(filaIndex, "Sku", "NOT_FOUND", "SKU no encontrado al actualizar"));
                            }
                            else
                            {
                                prod.EditarDatos(
                                    nombre: nombre,
                                    unidadMedida: unidad,
                                    afectacionImpuesto: afectacion,
                                    tasaImpuesto: tasa,
                                    categoria: categoria,
                                    marca: null,
                                    precioVenta: null,
                                    centroDeCosto: null,
                                    peso: dto.Peso.HasValue ? new Peso(dto.Peso.Value) : null,
                                    codigoBarras: string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : new CodigoBarras(dto.CodigoBarras),
                                    codigoFabrica: string.IsNullOrWhiteSpace(dto.CodigoFabrica) ? null : new CodigoFabrica(dto.CodigoFabrica),
                                    tipo: TipoProducto.Bien,
                                    codigoSunat: string.IsNullOrWhiteSpace(dto.CodigoSUNAT) ? null : new CodigoSUNAT(dto.CodigoSUNAT),
                                    establecimientosAsignados: establecimientos,
                                    asignarATodosLosEstablecimientos: false,
                                    imagenPrincipalId: null,
                                    descripcion: dto.Descripcion,
                                    tipoExistencia: TipoExistencia.ProductosTerminados
                                );
                                await _productos.UpdateAsync(prod);
                                actualizadas++;
                            }
                        }
                        else
                        {
                            var producto = new ProductoSimple(
                                empresaId: empresaId,
                                moneda: moneda,
                                sku: sku,
                                nombre: nombre,
                                unidadMedida: unidad,
                                afectacionImpuesto: afectacion,
                                tasaImpuesto: tasa,
                                categoria: categoria,
                                establecimientosAsignados: establecimientos,
                                descripcion: dto.Descripcion,
                                marca: null,
                                precioVenta: null,
                                codigoSunat: string.IsNullOrWhiteSpace(dto.CodigoSUNAT) ? null : new CodigoSUNAT(dto.CodigoSUNAT),
                                centroDeCosto: null,
                                peso: dto.Peso.HasValue ? new Peso(dto.Peso.Value) : null,
                                codigoBarras: string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : new CodigoBarras(dto.CodigoBarras),
                                codigoFabrica: string.IsNullOrWhiteSpace(dto.CodigoFabrica) ? null : new CodigoFabrica(dto.CodigoFabrica),
                                tipo: TipoProducto.Bien,
                                tipoExistencia: TipoExistencia.ProductosTerminados,
                                asignarATodosLosEstablecimientos: false,
                                imagenPrincipalId: null
                            );
                            await _productos.AddAsync(producto, ct);
                            creadas++;
                        }
                    }
                    else // UpdateOnly
                    {
                        if (!exists)
                        {
                            switch (req.OnConflict)
                            {
                                case OnSkuConflict.Skip: omitidas++; break;
                                case OnSkuConflict.Error: conError++; errores.Add(new RowError(filaIndex, "Sku", "NOT_FOUND", "SKU no existe para actualizar")); break;
                                case OnSkuConflict.Update:
                                    // no aplica
                                    omitidas++; break;
                            }
                        }
                        else
                        {
                            var prod = await _productos.GetBySkuAsync(sku, empresaId);
                            if (prod is null) { conError++; errores.Add(new RowError(filaIndex, "Sku", "NOT_FOUND", "SKU no encontrado")); }
                            else
                            {
                                prod.EditarDatos(
                                    nombre: nombre,
                                    unidadMedida: unidad,
                                    afectacionImpuesto: afectacion,
                                    tasaImpuesto: tasa,
                                    categoria: categoria,
                                    marca: null,
                                    precioVenta: null,
                                    centroDeCosto: null,
                                    peso: dto.Peso.HasValue ? new Peso(dto.Peso.Value) : null,
                                    codigoBarras: string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : new CodigoBarras(dto.CodigoBarras),
                                    codigoFabrica: string.IsNullOrWhiteSpace(dto.CodigoFabrica) ? null : new CodigoFabrica(dto.CodigoFabrica),
                                    tipo: TipoProducto.Bien,
                                    codigoSunat: string.IsNullOrWhiteSpace(dto.CodigoSUNAT) ? null : new CodigoSUNAT(dto.CodigoSUNAT),
                                    establecimientosAsignados: establecimientos,
                                    asignarATodosLosEstablecimientos: false,
                                    imagenPrincipalId: null,
                                    descripcion: dto.Descripcion,
                                    tipoExistencia: TipoExistencia.ProductosTerminados
                                );
                                await _productos.UpdateAsync(prod);
                                actualizadas++;
                            }
                        }
                    }

                    procesadas++;
                    toCommit++;

                    if (!req.ValidateOnly && toCommit >= batchSize)
                    {
                        await _uow.CommitAsync();
                        toCommit = 0;
                    }
                }
                catch (Exception ex)
                {
                    conError++;
                    errores.Add(new RowError(filaIndex, null, ex.GetType().Name, ex.Message));
                }
            }

            if (!req.ValidateOnly && toCommit > 0)
                await _uow.CommitAsync();

            sw.Stop();

            var resumen = new Summary(total, procesadas, creadas, actualizadas, omitidas, conError, sw.ElapsedMilliseconds);
            return new Response(resumen, errores);
        }

        private static decimal? ParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (decimal.TryParse(s.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                return v;
            return null;
        }

        private static List<string> ParseEstablecimientos(string? cell)
        {
            if (string.IsNullOrWhiteSpace(cell)) return new List<string>();
            return cell.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        private static TasaImpuesto TasaObligatoria(ProductoImportacionDto dto)
        {
            // Intenta tomar columna "TasaImpuesto" si existe en DTO (no defined) - por ahora defaults a 18%
            return TasaImpuesto.FromPercent(18m);
        }

        private static string GetCellValue(Dictionary<string, string?> row, List<string> detectedHeaders, string header)
        {
            // Busca header case-insensitive
            var match = detectedHeaders.FirstOrDefault(h => string.Equals(h, header, StringComparison.OrdinalIgnoreCase));
            if (match is null) return string.Empty;
            if (row.TryGetValue(match, out var v)) return v ?? string.Empty;
            // también intentar por la clave original sin normalizar
            if (row.TryGetValue(header, out var v2)) return v2 ?? string.Empty;
            return string.Empty;
        }
    }
}
