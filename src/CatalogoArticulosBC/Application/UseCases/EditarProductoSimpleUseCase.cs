using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.EditarProductoSimple
{
    public interface IEditarProductoSimpleUseCase
    {
        Task<EditarProductoSimpleOutputDto> ExecuteAsync(EditarProductoSimpleInputDto input, CancellationToken ct = default);
    }

    public sealed class EditarProductoSimpleUseCase : IEditarProductoSimpleUseCase
    {
        private readonly IProductoRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EditarProductoSimpleUseCase(
            IProductoRepository repo,
            IUnitOfWork uow,
            ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EditarProductoSimpleOutputDto> ExecuteAsync(EditarProductoSimpleInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Cargar agregado
            var producto = await _repo.GetByIdAsync(input.ProductoId, _tenant.EmpresaId);
            if (producto is null)
                throw new NotFoundException(nameof(ProductoSimple), input.ProductoId.ToString());

            // 2) Mapear DTO → VOs (usamos las fábricas/constructores del dominio)
            var nombre           = new NombreProducto(input.Nombre);
            var unidadMedida     = UnidadDeMedida.From(input.UnidadMedidaCodigo);
            var afectacion       = AfectacionImpuesto.From(input.AfectacionImpuestoCodigo);
            var tasa             = TasaImpuesto.FromPercent(input.TasaImpuestoPorcentaje);
            var categoriaId      = CategoriaId.FromString(input.CategoriaId);
            var marca            = string.IsNullOrWhiteSpace(input.MarcaNombre) ? null : new Marca(input.MarcaNombre);
            var codigoSunat      = string.IsNullOrWhiteSpace(input.CodigoSunat) ? null : new CodigoSUNAT(input.CodigoSunat);
            var codigoBarras     = string.IsNullOrWhiteSpace(input.CodigoBarras) ? null : new CodigoBarras(input.CodigoBarras);
            var codigoFabrica    = string.IsNullOrWhiteSpace(input.CodigoFabrica) ? null : new CodigoFabrica(input.CodigoFabrica);
            var peso             = input.PesoKg.HasValue ? new Peso(input.PesoKg.Value) : null;

            CentroDeCosto? centroCosto = null;
            if (!string.IsNullOrWhiteSpace(input.CentroDeCostoCodigo) || !string.IsNullOrWhiteSpace(input.CentroDeCostoNombre))
            {
                if (string.IsNullOrWhiteSpace(input.CentroDeCostoCodigo) || string.IsNullOrWhiteSpace(input.CentroDeCostoNombre))
                    throw new ArgumentException("Centro de costo requiere 'Código' y 'Nombre'.");
                centroCosto = new CentroDeCosto(input.CentroDeCostoCodigo!, input.CentroDeCostoNombre!);
            }

            // PrecioVenta opcional; si viene, usar Moneda del agregado + nueva afectación
            PrecioVenta? precioVenta = null;
            if (input.PrecioVentaMonto.HasValue)
            {
                precioVenta = new PrecioVenta(
                    monto: input.PrecioVentaMonto.Value,
                    moneda: producto.Moneda,
                    afectacionImpuesto: afectacion,
                    incluyeIGV: input.PrecioIncluyeIGV
                );
            }

            // Establecimientos (requeridos por el agregado)
            if (input.EstablecimientosAsignados is null || input.EstablecimientosAsignados.Count == 0)
                throw new ArgumentException("Debe asignar al menos un establecimiento.", nameof(input.EstablecimientosAsignados));

            var establecimientos = input.EstablecimientosAsignados
                .Select(EstablecimientoId.From)
                .ToList();

            // Tipo de existencia: si no viene, conservar la actual
            var tipoExistencia = input.TipoExistencia ?? producto.TipoExistencia;

            // 3) Editar datos (limpiamos eventos previos para aserciones limpias si quieres observarlos)
            producto.ClearDomainEvents();

            producto.EditarDatos(
                nombre: nombre,
                unidadMedida: unidadMedida,
                afectacionImpuesto: afectacion,
                tasaImpuesto: tasa,
                categoriaId: categoriaId,
                marca: marca,
                precioVenta: precioVenta,
                centroDeCosto: centroCosto,
                peso: peso,
                codigoBarras: codigoBarras,
                codigoFabrica: codigoFabrica,
                tipo: input.TipoProducto,
                codigoSunat: codigoSunat,
                establecimientosAsignados: establecimientos,
                asignarATodosLosEstablecimientos: input.AsignarATodosLosEstablecimientos,
                imagenPrincipalId: input.ImagenPrincipalId,
                descripcion: input.Descripcion,
                tipoExistencia: tipoExistencia,
                precioCompraDecimal: input.PrecioCompraMonto,
                porcentajeGananciaDecimal: input.PorcentajeGanancia,
                alias: input.Alias
            );

            // 4) (Opcional) Cambiar SKU si viene y es distinto
            if (!string.IsNullOrWhiteSpace(input.NuevoSku))
            {
                var nuevoSku = Sku.Crear(input.NuevoSku);

                if (!nuevoSku.Equals(producto.Sku))
                {
                    // Validar unicidad por empresa (tenant/empresa actual)
                    var exists = await _repo.ExisteSkuAsync(nuevoSku, _tenant.EmpresaId, ct);
                    if (exists)
                        throw new BusinessRuleException("El SKU ya existe para la empresa actual.");

                    producto.AsignarSku(nuevoSku);
                }
            }

            // 5) Persistir
            await _repo.UpdateAsync(producto);
            await _uow.CommitAsync();

            // 6) Salida
            return new EditarProductoSimpleOutputDto
            {
                ProductoId = producto.ProductoId,
                Sku = producto.Sku.Valor,
                Nombre = producto.Nombre.Valor,
                Habilitado = producto.Habilitado,
                TipoProducto = producto.Tipo,
                CategoriaId = producto.CategoriaId?.ToString(),
                CategoriaNombre = producto.CategoriaNombreSnapshot,
                CategoriaColor = producto.CategoriaColorSnapshot,
                AfectacionImpuestoCodigo = producto.AfectacionImpuesto.Codigo,
                TasaImpuestoFraccion = producto.TasaImpuesto.Fraccion,
                PrecioVentaMonto = producto.PrecioVenta?.Monto,
                PrecioIncluyeIGV = producto.PrecioVenta?.IncluyeIGV,
                MonedaCodigo = producto.Moneda.Codigo,
                TipoExistencia = producto.TipoExistencia,
                EstablecimientosAsignados = establecimientos.Select(e => (Guid)e).ToList(),
                PrecioCompraMonto = producto.PrecioCompra?.Monto,
                PrecioCompraMoneda = producto.PrecioCompra?.Moneda?.Codigo,
                PorcentajeGanancia = producto.PorcentajeGanancia?.Valor,
                Alias = producto.Alias?.Valor
            };
        }
    }
}
