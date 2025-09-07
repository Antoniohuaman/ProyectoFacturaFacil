using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductosSeleccionados
{
    public interface IEliminarProductosSeleccionadosUseCase
    {
        Task<EliminarProductosSeleccionadosOutputDto> ExecuteAsync(
            EliminarProductosSeleccionadosInputDto input,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Elimina en una sola operación (transacción/UoW) un conjunto de productos
    /// referidos por Id y/o por SKU, limitando la operación al tenant/empresa actual.
    /// </summary>
    public sealed class EliminarProductosSeleccionadosUseCase : IEliminarProductosSeleccionadosUseCase
    {
        private readonly IProductoRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EliminarProductosSeleccionadosUseCase(
            IProductoRepository repo,
            IUnitOfWork uow,
            ITenantContext tenant)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EliminarProductosSeleccionadosOutputDto> ExecuteAsync(
            EliminarProductosSeleccionadosInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (!input.Confirmar)
                throw new BusinessRuleException("Debe confirmar explícitamente la eliminación de los productos seleccionados.");

            var idsSolicitados = (input.ProductoIds ?? Array.Empty<Guid>()).Where(g => g != Guid.Empty).Distinct().ToList();
            var skusSolicitadosRaw = (input.Skus ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            if (idsSolicitados.Count == 0 && skusSolicitadosRaw.Count == 0)
                throw new BusinessRuleException("Debe especificar al menos un ProductoId o un SKU.");

            // Empresa actual (multiempresa)
            var empresaId = _tenant.EmpresaId; // usado para auditoría/seguridad a nivel de repo/infra

            // Resolver SKUs a Ids (si vienen)
            var idsDesdeSkus = new List<Guid>(skusSolicitadosRaw.Count);
            var skusNoEncontrados = new List<string>();
            foreach (var skuTxt in skusSolicitadosRaw)
            {
                // Normaliza/valida el SKU con el VO
                if (!Sku.TryCrear(skuTxt, out var skuVo, out _))
                {
                    // SKU inválido se considera "no encontrado" a efectos de salida (no rompe el flujo)
                    skusNoEncontrados.Add(skuTxt.Trim());
                    continue;
                }

                var p = await _repo.GetBySkuAsync(skuVo!);
                if (p is null)
                {
                    skusNoEncontrados.Add(skuVo!.Valor);
                }
                else
                {
                    idsDesdeSkus.Add(p.ProductoId);
                }
            }

            // Unión de IDs de entrada + IDs resueltos por SKU (sin duplicados)
            var idsAEliminar = idsSolicitados.Concat(idsDesdeSkus).Distinct().ToList();

            // Eliminar en batch usando el método eficiente del repositorio
            int cantidadEliminada = await _repo.DeleteManyAsync(idsAEliminar, empresaId, ct);
            await _uow.CommitAsync();

            // Para el DTO: los eliminados son los primeros N idsAEliminar (no hay feedback de IDs exactos eliminados, solo cantidad)
            var idsEliminados = idsAEliminar.Take(cantidadEliminada).ToList();
            var idsNoEncontrados = idsAEliminar.Skip(cantidadEliminada).ToList();

            return new EliminarProductosSeleccionadosOutputDto
            {
                EmpresaId = empresaId.Value,
                CantidadSolicitada = idsAEliminar.Count,
                CantidadEliminada = cantidadEliminada,
                CantidadNoEncontrada = idsNoEncontrados.Count + skusNoEncontrados.Count,
                IdsEliminados = idsEliminados,
                IdsNoEncontrados = idsNoEncontrados,
                SkusNoEncontrados = skusNoEncontrados,
                EjecutadoEnUtc = DateTimeOffset.UtcNow,
                Exitoso = true
            };
        }
    }
}
