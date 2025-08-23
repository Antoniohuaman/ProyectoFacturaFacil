using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio
using SharedKernel.Exceptions;               // NotFoundException

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Renombrar una columna de la lista de precios ACTIVA.
    /// Reglas operativas:
    ///  - Debe existir lista activa.
    ///  - Debe existir la columna indicada.
    ///  - El agregado ListaPrecio valida invariantes (nombre válido, posibles duplicados, etc.).
    ///  - Concurrencia optimista con expectedVersion.
    /// </summary>
    public sealed class RenombrarColumnaUseCase
    {
        public readonly record struct Request(
            byte ColumnaNumero,
            string NuevoNombre,
            string? Usuario = null,
            DateTimeOffset? Cuando = null
        );

        public readonly record struct Response(
            byte ColumnaNumero,
            string Nombre,
            int Version
        );

        private readonly IListaPrecioRepository _listaRepo;
        private readonly IUnitOfWork _uow;

        public RenombrarColumnaUseCase(
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
            var existeColumna = lista.Plantilla
                                     .Columnas
                                     .Any(c => c.Id.Equals(colId));
            if (!existeColumna)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            var expectedVersion = lista.Version; // antes de mutar

            // 3) Mutar el agregado (dominio valida nombre/invariantes)
            var nuevoNombre = NombreColumnaPrecio.Crear(req.NuevoNombre);
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;

            lista.RenombrarColumna(colId, nuevoNombre, req.Usuario, cuando);

            // 4) Persistencia + UoW
            await _listaRepo.GuardarAsync(lista, expectedVersion, ct);
            await _uow.SaveChangesAsync(ct);

            // 5) Respuesta
            return new Response(
                ColumnaNumero: req.ColumnaNumero,
                Nombre: nuevoNombre.Valor,
                Version: lista.Version
            );
        }
    }
}
