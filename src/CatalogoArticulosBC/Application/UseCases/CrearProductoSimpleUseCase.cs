using ConfiguracionSistemaBC.Domain.Aggregates;
using SharedKernel.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.Specifications;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso para crear un nuevo ProductoSimple.
    /// Aplica validaciones de dominio y publica eventos.
    /// </summary>
    public class CrearProductoSimpleUseCase
    {
    private readonly IProductoRepository _repository;
    private readonly IValidadorUnicidadSku _skuValidator;
    private readonly IEventBus _eventBus;
    private readonly ConfiguracionSistemaBC.Domain.Aggregates.ConfiguracionEmpresa _configuracionEmpresa;

        public CrearProductoSimpleUseCase(
            IProductoRepository repository,
            IValidadorUnicidadSku skuValidator,
            IEventBus eventBus,
            ConfiguracionSistemaBC.Domain.Aggregates.ConfiguracionEmpresa configuracionEmpresa)
        {
            _repository = repository;
            _skuValidator = skuValidator;
            _eventBus = eventBus;
            _configuracionEmpresa = configuracionEmpresa;
        }

        /// <summary>
        /// Ejecuta el caso de uso. Retorna el ID del nuevo producto.
        /// </summary>
        public async Task<Guid> Handle(CrearProductoSimpleDto dto)
        {
            // Mapear DTO a VOs
            var sku = new SKU(dto.Sku!);
            var nombre = new NombreProducto(dto.Nombre!);
            var unidad = UnidadDeMedida.From(dto.UnidadMedida!);
            var afectacion = AfectacionImpuesto.From(dto.AfectacionIgvCodigo!);
            var categoria = new Categoria(dto.Categoria!);
            var marca = dto.Marca is null ? null : new Marca(dto.Marca);


            // Obtener moneda desde la configuración de empresa (debe estar disponible en el contexto)
            var moneda = _configuracionEmpresa.MonedaBase;
            // Precio incluyendo afectación IGV
            var precio = dto.PrecioVenta.HasValue
                ? new PrecioVenta(
                    dto.PrecioVenta.Value,
                    moneda,
                    afectacion,
                    incluyeIGV: true)
                : null;


            var codigoDet = dto.TieneDetraccion && dto.CodigoDetraccion is not null
                ? CodigoDetraccion.FromCodigo(dto.CodigoDetraccion)
                : null;

            var sunat = dto.CodigoSunat is null ? null : new CodigoSUNAT(dto.CodigoSunat);
            // var baseVenta = dto.BaseImponibleVentas.HasValue ? new BaseImponibleVentas(dto.BaseImponibleVentas.Value) : null;
            var centroDeCosto = CentroDeCosto.FromOptional(dto.CentroCosto, dto.CentroCosto);
            var peso = dto.Peso.HasValue ? new Peso(dto.Peso.Value) : null;
            // ...existing code...
            var cb = dto.CodigoBarras is null ? null : new CodigoBarras(dto.CodigoBarras);
            var cf = dto.CodigoFabrica is null ? null : new CodigoFabrica(dto.CodigoFabrica);
            // ...existing code...
            var fechaVen = dto.FechaVencimiento.HasValue ? new FechaVencimiento(dto.FechaVencimiento.Value) : null;

            // Tipos
            var tipoProductoEnum = Enum.Parse<TipoProducto>(dto.TipoProducto!);
            var tipoExistenciaEnum = Enum.Parse<TipoExistencia>(dto.TipoExistencia!);

            // Crear agregado

            var producto = new ProductoSimple(
                moneda,
                sku,
                nombre,
                unidad,
                afectacion,
                categoria,
                dto.AlmacenesAsignados,
                dto.Descripcion,
                marca,
                precio,
                dto.TieneDetraccion,
                codigoDet,
                sunat,
                // baseVenta eliminado,
                centroDeCosto,
                peso,
                // ...existing code...
                cb,
                cf,
                // ...existing code...
                tipoProductoEnum,
                tipoExistenciaEnum,
                fechaVen,
                dto.AsignarATodosLosAlmacenes,
                dto.ImagenPrincipalId);

            // Validar reglas de negocio
            var datosMinSpec = new ProductoDatosMinimosSpecification();
            var minResult = datosMinSpec.IsSatisfiedBy(producto);
            if (!minResult.IsSatisfied)
                throw new ArgumentException(minResult.Message);

            var skuSpec = new SkuUnicoSpecification(_skuValidator);
            var skuResult = skuSpec.IsSatisfiedBy(producto);
            if (!skuResult.IsSatisfied)
                throw new ArgumentException(skuResult.Message);

            // Persistir
            await _repository.AddAsync(producto);

            // Publicar eventos de dominio
            foreach (var ev in producto.DomainEvents)
            {
                await _eventBus.Publish(ev);
            }
            producto.ClearDomainEvents();

            return producto.ProductoId;
        }
    }
}
