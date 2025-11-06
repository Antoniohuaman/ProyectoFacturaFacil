using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio
using SharedKernel.Exceptions;               // NotFoundException
using SharedKernel.Application.Interfaces;   // ITenantContext

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
    private readonly ITenantContext _tenant;

        public RenombrarColumnaUseCase(
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
            await _listaRepo.GuardarAsync(lista, empresaId, null, expectedVersion, ct);
            await _uow.CommitAsync(ct);

            // 5) Respuesta
            return new Response(
                ColumnaNumero: req.ColumnaNumero,
                Nombre: nuevoNombre.Valor,
                Version: lista.Version
            );
        }
    }
}
