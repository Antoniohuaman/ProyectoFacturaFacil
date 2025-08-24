using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using SharedKernel.Exceptions;               // NotFoundException

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Consulta: Obtiene la plantilla (todas las columnas) de la Lista de Precios ACTIVA.
    /// Reglas:
    ///  - Debe existir lista activa.
    ///  - No hay mutaciones (no requiere UnitOfWork).
    ///  - Devuelve columnas ordenadas por 'Orden'.
    /// </summary>
    public sealed class ObtenerPlantillaUseCase
    {
        public readonly record struct Request();

        public readonly record struct ColumnaDto(
            string Nombre,
            string Modo,   // "Fijo" | "PorVolumen" (ToString del enum del dominio)
            bool EsBase,
            bool Visible,
            byte Orden
        );

        public readonly record struct Response(
            int CantidadColumnas,
            ColumnaDto[] Columnas,
            int Version
        );

        private readonly IListaPrecioRepository _listaRepo;

        public ObtenerPlantillaUseCase(IListaPrecioRepository listaRepo)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            // 1) Traer lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Mapear columnas ordenadas por 'Orden'
            var columnas = lista.Plantilla
                                .Columnas
                                .OrderBy(c => c.Orden)
                                .Select(c => new ColumnaDto(
                                    Nombre: c.Nombre.Valor,
                                    Modo: c.Modo.ToString(),
                                    EsBase: c.EsBase,
                                    Visible: c.Visible,
                                    Orden: c.Orden
                                ))
                                .ToArray();

            // 3) Respuesta
            return new Response(
                CantidadColumnas: columnas.Length,
                Columnas: columnas,
                Version: lista.Version
            );
        }
    }
}
