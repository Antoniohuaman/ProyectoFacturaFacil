using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Application.UseCases.ConsultarProducto;
using CatalogoArticulosBC.Domain.Entities;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.ConsultarProducto
{
    public interface IConsultarProductoUseCase
    {
        Task<ConsultarProductoOutputDto> ExecuteAsync(ConsultarProductoInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Consulta el detalle de un producto identificado por Id, SKU o Nombre.
    /// No realiza mutaciones (read-only).
    /// </summary>
    public sealed class ConsultarProductoUseCase : IConsultarProductoUseCase
    {
        private readonly IProductoRepository _repo;
        private readonly ITenantContext _tenant;

        public ConsultarProductoUseCase(IProductoRepository repo, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ConsultarProductoOutputDto> ExecuteAsync(ConsultarProductoInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            ProductoSimple? producto = null;

            // Resolución de identificador en orden de prioridad: Id -> SKU -> Nombre
            if (input.ProductoId.HasValue && input.ProductoId.Value != Guid.Empty)
            {
                producto = await _repo.GetByIdAsync(input.ProductoId.Value, _tenant.EmpresaId);
            }
            else if (!string.IsNullOrWhiteSpace(input.Sku))
            {
                var sku = Sku.Crear(input.Sku!.Trim());
                producto = await _repo.GetBySkuAsync(sku, _tenant.EmpresaId);
            }
            else if (!string.IsNullOrWhiteSpace(input.Nombre))
            {
                producto = await _repo.GetByNombreAsync(input.Nombre!.Trim(), _tenant.EmpresaId);
            }
            else
            {
                throw new BusinessRuleException("Debe proporcionar ProductoId, SKU o Nombre para consultar el producto.");
            }

            if (producto is null)
                throw new NotFoundException("No se encontró el producto con los criterios proporcionados.");

            // Multimedia (opcional)

            IReadOnlyCollection<MultimediaProducto> multimedia = Array.Empty<MultimediaProducto>();
            if (input.IncluirMultimedia)
            {
                var list = await _repo.GetMultimediaByProductoIdAsync(producto.ProductoId);
                if (list != null)
                    multimedia = list.ToList();
                // Si list es null, multimedia ya está inicializado vacío
            }

            // Construir DTO de salida
            var salida = ConsultarProductoOutputDto.From(producto, multimedia, _tenant.EmpresaId.Value);
            return salida;
        }
    }
}
