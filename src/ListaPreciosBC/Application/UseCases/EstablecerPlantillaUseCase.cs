using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna, ConfiguracionColumnaPrecio
using SharedKernel.Exceptions;               // NotFoundException, BusinessRuleException
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Establece (reemplaza) la plantilla de columnas de la Lista de Precios ACTIVA.
    /// Reglas operativas del UC:
    ///  - Debe existir lista activa.
    ///  - Se construyen las Configuraciones de Columna como VO y se delega al agregado
    ///    la validación de invariantes (exactamente una base, orden/visibilidad, etc.).
    ///  - Concurrencia optimista con expectedVersion.
    /// </summary>
    public sealed class EstablecerPlantillaUseCase
    {
        public readonly record struct Columna(
            byte Numero,
            string Nombre,
            ModoValorizacionColumna Modo,
            bool EsBase,
            bool Visible,
            byte Orden
        );

        public readonly record struct Request(
            IReadOnlyList<Columna> Columnas,
            string? Usuario = null,
            DateTimeOffset? Cuando = null
        );

        public readonly record struct Response(
            int CantidadColumnas,
            byte ColumnaBaseNumero,
            int Version
        );

    private readonly IListaPrecioRepository _listaRepo;
        private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;

        public EstablecerPlantillaUseCase(
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

            // 2) Mapear DTO -> VO ConfiguracionColumnaPrecio
            if (req.Columnas is null || req.Columnas.Count == 0)
                throw new BusinessRuleException("La plantilla debe contener al menos una columna.");

            var configuraciones = new List<ConfiguracionColumnaPrecio>(req.Columnas.Count);
            foreach (var c in req.Columnas)
            {
                var id = IdentificadorColumnaPrecio.DesdeNumero(c.Numero);
                var nombre = NombreColumnaPrecio.Crear(c.Nombre);
                var cfg = ConfiguracionColumnaPrecio.Crear(
                    id,
                    nombre,
                    c.Modo,
                    esBase: c.EsBase,
                    visible: c.Visible,
                    orden: c.Orden
                );
                configuraciones.Add(cfg);
            }

            // 3) Concurrencia
            var expectedVersion = lista.Version;

            // 4) Mutación de dominio (el agregado valida invariantes)
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
            var plantilla = PlantillaColumnasPrecio.Crear(configuraciones);
            lista.EstablecerPlantilla(plantilla, req.Usuario, cuando);

            // 5) Persistencia + UoW
            await _listaRepo.GuardarAsync(lista, empresaId, null, expectedVersion, ct);
            await _uow.CommitAsync(ct);

            // 6) Respuesta (usamos el número marcado como base en el request)
            var baseNumero = req.Columnas.Single(x => x.EsBase).Numero;
            return new Response(
                CantidadColumnas: req.Columnas.Count,
                ColumnaBaseNumero: baseNumero,
                Version: lista.Version
            );
        }
    }
}
