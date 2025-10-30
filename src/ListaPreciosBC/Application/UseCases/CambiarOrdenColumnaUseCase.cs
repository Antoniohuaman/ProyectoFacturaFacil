using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio
using SharedKernel.Exceptions;               // NotFoundException, BusinessRuleException
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Cambiar el ORDEN de una columna en la Lista de Precios ACTIVA.
    /// Reglas operativas del UC:
    ///  - Debe existir lista activa.
    ///  - Debe existir la columna indicada.
    ///  - Se valida que el nuevo orden sea >= 1 (rango superior lo valida el dominio).
    ///  - El agregado ListaPrecio aplica las invariantes (ajustes de orden, unicidad, etc.).
    ///  - Concurrencia optimista con expectedVersion.
    /// </summary>
    public sealed class CambiarOrdenColumnaUseCase
    {
        public readonly record struct Request(
            byte ColumnaNumero,
            byte NuevoOrden,
            string? Usuario = null,
            DateTimeOffset? Cuando = null
        );

        public readonly record struct Response(
            byte ColumnaNumero,
            byte Orden,
            int Version
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;

        public CambiarOrdenColumnaUseCase(
            IListaPrecioRepository listaRepo,
            IUnitOfWork uow,
            ITenantContext tenant)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            // 0) Contexto
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null) throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            // 1) Traer lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Validar existencia de la columna
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var existeColumna = lista.Plantilla
                                     .Columnas
                                     .Any(c => c.Id.Equals(colId));
            if (!existeColumna)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            // 3) Validación simple del orden (el dominio valida resto de invariantes)
            if (req.NuevoOrden < 1)
                throw new BusinessRuleException("El orden debe ser mayor o igual a 1.");

            var expectedVersion = lista.Version; // antes de mutar

            // 4) Mutación de dominio
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
            lista.CambiarOrdenColumna(colId, req.NuevoOrden, req.Usuario, cuando);

            // 5) Persistencia + UoW
            await _listaRepo.GuardarAsync(lista, empresaId, null, expectedVersion, ct);
            await _uow.SaveChangesAsync(ct);

            // 6) Respuesta
            return new Response(
                ColumnaNumero: req.ColumnaNumero,
                Orden: req.NuevoOrden,
                Version: lista.Version
            );
        }
    }
}
