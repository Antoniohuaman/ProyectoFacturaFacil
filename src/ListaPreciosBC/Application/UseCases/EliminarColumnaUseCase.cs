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
    /// Caso de uso: Eliminar una columna de la Lista de Precios ACTIVA.
    /// Reglas operativas:
    ///  - Debe existir lista activa.
    ///  - Debe existir la columna indicada.
    ///  - No se permite eliminar la columna BASE.
    ///  - El agregado ListaPrecio aplica invariantes adicionales (reordenamiento, referencias, etc.).
    ///  - Concurrencia optimista con expectedVersion.
    /// </summary>
    public sealed class EliminarColumnaUseCase
    {
        public readonly record struct Request(
            byte ColumnaNumero,
            string? Usuario = null,
            DateTimeOffset? Cuando = null
        );

        public readonly record struct Response(
            byte ColumnaNumero,
            int Version
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;

        public EliminarColumnaUseCase(
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

            // 1) Lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Buscar columna
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var columna = lista.Plantilla
                               .Columnas
                               .SingleOrDefault(c => c.Id.Equals(colId));
            if (columna is null)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            // 3) Regla operativa: no permitir eliminar la base (el dominio podría validarlo, pero fallamos pronto)
            if (columna.EsBase)
                throw new BusinessRuleException("No se puede eliminar la columna base.");

            var expectedVersion = lista.Version; // capturar antes de mutar

            // 4) Mutación de dominio
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
            lista.EliminarColumna(colId, req.Usuario, cuando);

            // 5) Persistencia
            await _listaRepo.GuardarAsync(lista, empresaId, null, expectedVersion, ct);
            await _uow.SaveChangesAsync(ct);

            // 6) Respuesta
            return new Response(
                ColumnaNumero: req.ColumnaNumero,
                Version: lista.Version
            );
        }
    }
}
