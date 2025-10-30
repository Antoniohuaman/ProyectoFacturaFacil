using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio
using SharedKernel.Exceptions;               // NotFoundException
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Marcar una columna como BASE en la Lista de Precios ACTIVA.
    /// Reglas operativas:
    ///  - Debe existir lista activa.
    ///  - Debe existir la columna indicada.
    ///  - El agregado ListaPrecio aplica las invariantes: exactamente una columna base.
    ///  - Concurrencia optimista con expectedVersion.
    ///  - Idempotente si ya es base (el agregado decide si emite evento o no).
    /// </summary>
    public sealed class MarcarColumnaComoBaseUseCase
    {
        public readonly record struct Request(
            byte ColumnaNumero,
            string? Usuario = null,
            DateTimeOffset? Cuando = null
        );

        public readonly record struct Response(
            byte ColumnaNumeroBase,
            int Version
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;

        public MarcarColumnaComoBaseUseCase(
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

            // 2) Validar existencia de la columna (fail fast). El agregado también lo valida.
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var existeColumna = lista.Plantilla
                                     .Columnas
                                     .Any(c => c.Id.Equals(colId));
            if (!existeColumna)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            var expectedVersion = lista.Version; // antes de mutar

            // 3) Mutación de dominio (nombre del método según tu aggregate)
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
            lista.MarcarColumnaComoBase(colId, req.Usuario, cuando);

            // 4) Persistencia + UoW
            await _listaRepo.GuardarAsync(lista, empresaId, null, expectedVersion, ct);
            await _uow.SaveChangesAsync(ct);

            // 5) Respuesta
            return new Response(
                ColumnaNumeroBase: req.ColumnaNumero,
                Version: lista.Version
            );
        }
    }
}
