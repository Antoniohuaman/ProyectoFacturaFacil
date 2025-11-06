using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.ValueObjects;    // Sku, IdentificadorColumnaPrecio
using SharedKernel.ValueObjects;             // Sku
using SharedKernel.Exceptions;               // NotFoundException
using SharedKernel.Application.Interfaces;   // ITenantContext
using ListaPreciosBC.Application.Interfaces; // ICatalogoReadModel

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Exporta a CSV (compatible con Excel) los precios vigentes por columna para un SKU,
    /// en una fecha y cantidad dadas. No requiere UoW (consulta).
    /// Columnas del CSV: SKU;Fecha;Cantidad;ColumnaNumero;Nombre;Modo;EsBase;Visible;Monto;IncluyeImpuesto;Moneda
    /// </summary>
    public sealed class ExportarPreciosSkuExcelUseCase
    {
        public readonly record struct Request(
            string Sku,
            int Cantidad,
            DateTimeOffset? Fecha = null,
            bool SoloVisibles = false
        );

        public readonly record struct Response(
            string NombreArchivo,
            string ContentType,
            byte[] Contenido,
            int ColumnasIncluidas,
            int VersionAgregado
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly IPrecioProductoRepository _precioRepo;
    private readonly ITenantContext _tenant;
    private readonly ICatalogoReadModel _catalogo;

        public ExportarPreciosSkuExcelUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            ITenantContext tenant,
            ICatalogoReadModel catalogo)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
            _precioRepo = precioRepo ?? throw new ArgumentNullException(nameof(precioRepo));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
        }

        // Backward-compatible overload for tests that didn't provide a catalog
        public ExportarPreciosSkuExcelUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            ITenantContext tenant)
            : this(listaRepo, precioRepo, tenant, new NullCatalogoReadModel())
        { }

        private sealed class NullCatalogoReadModel : ICatalogoReadModel
        {
            public Task<SharedKernel.ValueObjects.ProductoId?> TryGetProductoIdBySkuAsync(SharedKernel.ValueObjects.EmpresaId empresaId, string sku, CancellationToken ct = default)
                => Task.FromResult<SharedKernel.ValueObjects.ProductoId?>(null);
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            // 0) Contexto
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null) throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            // 1) Lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Resolver ProductoId y obtener agregado
            var sku = Sku.Crear(req.Sku);
            var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, sku.Valor, ct)
                             ?? throw new NotFoundException("Producto", sku.Valor);
            var agregado = await _precioRepo.ObtenerPorProductoIdAsync(empresaId, null, productoId, ct);
            if (agregado is null)
                throw new NotFoundException($"No existe PrecioProducto para el SKU {req.Sku}.");

            // 3) Fecha
            var fecha = req.Fecha ?? DateTimeOffset.UtcNow;

            // 4) Columnas a exportar
            var columnas = lista.Plantilla.Columnas
                                   .Where(c => !req.SoloVisibles || c.Visible)
                                   .OrderBy(c => c.Orden)
                                   .ToArray();

            // 5) Construir CSV (Excel-friendly)
            var sb = new StringBuilder(capacity: 1024);
            const char sep = ';';
            var ci = CultureInfo.InvariantCulture;

            // Header
            sb.Append("SKU").Append(sep)
              .Append("Fecha").Append(sep)
              .Append("Cantidad").Append(sep)
              .Append("ColumnaNumero").Append(sep)
              .Append("Nombre").Append(sep)
              .Append("Modo").Append(sep)
              .Append("EsBase").Append(sep)
              .Append("Visible").Append(sep)
              .Append("Monto").Append(sep)
              .Append("IncluyeImpuesto").Append(sep)
              .Append("Moneda")
              .AppendLine();

            // Rows
            foreach (var col in columnas)
            {

                var resuelto = agregado.ObtenerPrecioVigente(col.Id, fecha, req.Cantidad);
                var montoStr   = resuelto is null ? "" : resuelto.Valor.Monto.ToString(ci);
                var incluyeStr = resuelto is null ? "" : resuelto.Valor.IncluyeImpuesto.ToString(ci);
                var monedaStr  = resuelto is null ? "" : resuelto.Valor.Importe.Moneda.Codigo;

                // CSV values
                sb.Append(sku.Valor).Append(sep)
                  .Append(fecha.ToString("yyyy-MM-ddTHH:mm:ss", ci)).Append(sep)
                  .Append(req.Cantidad.ToString(ci)).Append(sep)
                  .Append(col.Id.Numero.ToString(ci)).Append(sep)
                  .Append(Escape(col.Nombre.Valor)).Append(sep)
                  .Append(col.Modo.ToString()).Append(sep)
                  .Append(col.EsBase.ToString(ci)).Append(sep)
                  .Append(col.Visible.ToString(ci)).Append(sep)
                  .Append(montoStr).Append(sep)
                  .Append(incluyeStr).Append(sep)
                  .Append(monedaStr)
                  .AppendLine();
            }

            // 6) Empaquetar archivo
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var safeSku = SanitizeForFileName(sku.Valor);
            var fileName = $"precios_{safeSku}_{fecha:yyyyMMddHHmmss}.csv";

            return new Response(
                NombreArchivo: fileName,
                ContentType: "text/csv; charset=utf-8",
                Contenido: bytes,
                ColumnasIncluidas: columnas.Length,
                VersionAgregado: agregado.Version
            );
        }

        private static string Escape(string value)
        {
            // Si contiene separador o comillas, envolver en comillas dobles y duplicar comillas internas
            if (value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0)
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static string SanitizeForFileName(string value)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var cleansed = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(cleansed) ? "sku" : cleansed;
        }
    }
}
