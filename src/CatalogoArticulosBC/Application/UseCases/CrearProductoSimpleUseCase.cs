using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.Services;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.CrearProductoSimple
{
    /// <summary>
    /// Caso de uso para crear un ProductoSimple (Bien o Servicio).
    /// Reglas respetadas:
    /// - SKU único por empresa.
    /// - Coherencia Afectación/Tasa (el agregado también valida).
    /// - Mínimo 1 establecimiento asignado (el agregado valida).
    /// - Moneda debe provenir de Configuración de Empresa (inyectada/derivada en esta capa).
    /// </summary>
    public sealed class CrearProductoSimpleUseCase
    {
        private readonly IProductoRepository _productos;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly ISkuGenerator? _skuGenerator;   // opcional si permites autogenerar
        private readonly IEventBus? _eventBus;           // opcional si publicas aquí en vez de Outbox

        public CrearProductoSimpleUseCase(
            IProductoRepository productos,
            IUnitOfWork uow,
            ITenantContext tenant,
            ISkuGenerator? skuGenerator = null,
            IEventBus? eventBus = null)
        {
            _productos = productos ?? throw new ArgumentNullException(nameof(productos));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _skuGenerator = skuGenerator; // puede ser null (siempre que AutogenerarSku sea false)
            _eventBus = eventBus;         // puede ser null (si tienes Outbox en infraestructura)
        }

        public async Task<CrearProductoSimpleOutputDto> Handle(
            CrearProductoSimpleInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // ------------------- SKU -------------------
            Sku sku = input.AutogenerarSku
                ? (_skuGenerator ?? throw new InvalidOperationException("No hay ISkuGenerator disponible."))
                    .Generar()
                : Sku.Crear(input.Sku);

            // Unicidad por empresa (tenant)
            var empresaId = _tenant.EmpresaId; // SharedKernel.ValueObjects.EmpresaId
            if (await _productos.ExisteSkuAsync(sku, empresaId, ct))
                throw new BusinessRuleException($"El SKU '{sku.Valor}' ya existe para la empresa '{empresaId.Value}'.");

            // ------------------- VOs requeridos -------------------
            var nombre          = new NombreProducto(input.Nombre);
            var unidad          = UnidadDeMedida.From(input.UnidadMedidaCodigo);
            var afectacion      = AfectacionImpuesto.From(input.AfectacionImpuestoCodigo);
            var categoria       = new Categoria(input.Categoria);
            var moneda          = Moneda.Create(input.MonedaCodigoIso4217);

            // TasaImpuesto: si no grava -> 0; si grava -> debe venir (10 o 18)
            TasaImpuesto tasa = afectacion.GravaImpuesto
                ? CrearTasaGravadaObligatoria(input.TasaImpuestoPercent)
                : TasaImpuesto.Cero;

            // ------------------- Opcionales -------------------
            Marca? marca = string.IsNullOrWhiteSpace(input.Marca) ? null : new Marca(input.Marca.Trim());

            PrecioVenta? precioVenta = input.PrecioVentaMonto.HasValue
                ? new PrecioVenta(input.PrecioVentaMonto.Value, moneda, afectacion, input.PrecioIncluyeIGV)
                : null;

            CodigoSUNAT? codigoSUNAT = string.IsNullOrWhiteSpace(input.CodigoSUNAT) ? null : new CodigoSUNAT(input.CodigoSUNAT);

            CentroDeCosto? centroDeCosto = null;
            if (!string.IsNullOrWhiteSpace(input.CentroDeCostoCodigo) || !string.IsNullOrWhiteSpace(input.CentroDeCostoNombre))
            {
                if (string.IsNullOrWhiteSpace(input.CentroDeCostoCodigo) || string.IsNullOrWhiteSpace(input.CentroDeCostoNombre))
                    throw new ArgumentException("Centro de costo requiere 'Código' y 'Nombre'.");
                centroDeCosto = new CentroDeCosto(input.CentroDeCostoCodigo!, input.CentroDeCostoNombre!);
            }

            Peso? peso = input.PesoKg.HasValue ? new Peso(input.PesoKg.Value) : null;

            CodigoBarras? codigoBarras = string.IsNullOrWhiteSpace(input.CodigoBarras) ? null : new CodigoBarras(input.CodigoBarras);
            CodigoFabrica? codigoFabrica = string.IsNullOrWhiteSpace(input.CodigoFabrica) ? null : new CodigoFabrica(input.CodigoFabrica);

            var establecimientos = MapEstablecimientos(input.Establecimientos);

            var tipoExistencia = input.TipoExistencia
                ?? (input.Tipo == TipoProducto.Servicio ? TipoExistencia.Servicios : TipoExistencia.ProductosTerminados);

            // ------------------- Construcción del agregado -------------------
            var producto = new ProductoSimple(
                empresaId,
                moneda: moneda,
                sku: sku,
                nombre: nombre,
                unidadMedida: unidad,
                afectacionImpuesto: afectacion,
                tasaImpuesto: tasa,
                categoria: categoria,
                establecimientosAsignados: establecimientos,
                descripcion: input.Descripcion,
                marca: marca,
                precioVenta: precioVenta,
                codigoSunat: codigoSUNAT,
                centroDeCosto: centroDeCosto,
                peso: peso,
                codigoBarras: codigoBarras,
                codigoFabrica: codigoFabrica,
                tipo: input.Tipo,
                tipoExistencia: tipoExistencia,
                asignarATodosLosEstablecimientos: input.AsignarATodosLosEstablecimientos,
                imagenPrincipalId: input.ImagenPrincipalId
            );

            // ------------------- Persistencia + publicación de eventos -------------------
            await _productos.AddAsync(producto, ct);
            await _uow.CommitAsync();

            // Si decides publicar aquí (si no usas Outbox en infra):
            if (_eventBus is not null && producto.DomainEvents.Count > 0)
                await _eventBus.PublishAsync(producto.DomainEvents, ct);

            // ------------------- Salida -------------------
            return new CrearProductoSimpleOutputDto
            {
                ProductoId = producto.ProductoId,
                Sku = producto.Sku.Valor,
                Habilitado = producto.Habilitado,
                Tipo = producto.Tipo,
                TipoExistencia = producto.TipoExistencia,
                Nombre = producto.Nombre.Valor,
                Descripcion = producto.Descripcion,
                Categoria = producto.Categoria.Nombre,
                Moneda = producto.Moneda.Codigo,
                PrecioVentaMonto = producto.PrecioVenta?.Monto,
                PrecioIncluyeIGV = producto.PrecioVenta?.IncluyeIGV,
                AfectacionImpuestoCodigo = producto.AfectacionImpuesto.Codigo,
                TasaImpuestoPercent = producto.TasaImpuesto.Porcentaje,
                Establecimientos = producto.EstablecimientosAsignados.Select(e => e.Value).ToList(),
                AsignarATodosLosEstablecimientos = producto.AsignarATodosLosEstablecimientos,
                ImagenPrincipalId = producto.ImagenPrincipalId
            };
        }

        private static TasaImpuesto CrearTasaGravadaObligatoria(decimal? tasaPercent)
        {
            if (!tasaPercent.HasValue)
                throw new ArgumentException("Debe especificar la tasa de impuesto (10 o 18) para afectaciones gravadas.");

            var p = tasaPercent.Value;
            if (p != 10m && p != 18m)
                throw new ArgumentException("Solo se permite IGV 10% o 18% para afectaciones gravadas.");

            return TasaImpuesto.FromPercent(p);
        }

        private static List<EstablecimientoId> MapEstablecimientos(List<Guid> lista)
        {
            if (lista is null || lista.Count == 0)
                throw new ArgumentException("Debe asignar al menos un establecimiento.");

            var result = new List<EstablecimientoId>(lista.Count);
            foreach (var g in lista)
                result.Add(EstablecimientoId.From(g));
            return result;
        }
    }
}
