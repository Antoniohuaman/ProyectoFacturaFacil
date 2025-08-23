using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, ModoValorizacionColumna
using SharedKernel.Exceptions;               // NotFoundException, BusinessRuleException

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Cambiar el MODO de una columna (FIJO ↔ POR VOLUMEN) en la Lista de Precios ACTIVA.
    /// Reglas (operativas del UC):
    ///  - Debe existir lista activa.
    ///  - Debe existir la columna indicada.
    ///  - El agregado ListaPrecio valida invariantes de negocio (p.ej., si hay precios existentes incompatibles, etc.).
    ///  - Concurrencia optimista con expectedVersion.
    /// </summary>
    public sealed class CambiarModoColumnaUseCase
    {
        public readonly record struct Request(
            byte ColumnaNumero,
            ModoValorizacionColumna NuevoModo,
            string? Usuario = null,
            DateTimeOffset? Cuando = null
        );

        public readonly record struct Response(
            byte ColumnaNumero,
            string Modo, // “Fijo” | “PorVolumen”
            int Version
        );

        private readonly IListaPrecioRepository _listaRepo;
        private readonly IUnitOfWork _uow;

        public CambiarModoColumnaUseCase(
            IListaPrecioRepository listaRepo,
            IUnitOfWork uow)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            // 1) Traer lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Verificar que la columna exista (fail fast). El agregado también lo valida.
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var columna = lista.Plantilla
                               .Columnas
                               .SingleOrDefault(c => c.Id.Equals(colId));
            if (columna is null)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            var expectedVersion = lista.Version; // antes de mutar

            // 3) Mutar el agregado (el dominio valida invariantes del cambio de modo)
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
            lista.CambiarModoColumna(colId, req.NuevoModo, req.Usuario, cuando);

            // 4) Persistencia + UoW
            await _listaRepo.GuardarAsync(lista, expectedVersion, ct);
            await _uow.SaveChangesAsync(ct);

            // 5) Respuesta
            return new Response(
                ColumnaNumero: req.ColumnaNumero,
                Modo: req.NuevoModo.ToString(),
                Version: lista.Version
            );
        }
    }
}
