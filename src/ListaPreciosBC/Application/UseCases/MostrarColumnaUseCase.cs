using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio
using SharedKernel.Exceptions;               // NotFoundException

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso (consulta): Mostrar información de una columna de la Lista de Precios ACTIVA.
    /// Reglas:
    ///  - Debe existir lista activa.
    ///  - Debe existir la columna indicada.
    ///  - No requiere UoW porque no hay mutaciones.
    /// </summary>
    public sealed class MostrarColumnaUseCase
    {
        public readonly record struct Request(
            byte ColumnaNumero
        );

        public readonly record struct Response(
            byte ColumnaNumero,
            string Nombre,
            string Modo,   // "Fijo" | "PorVolumen" (ToString del enum)
            bool EsBase,
            bool Visible,
            byte Orden
        );

        private readonly IListaPrecioRepository _listaRepo;

        public MostrarColumnaUseCase(IListaPrecioRepository listaRepo)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            // 1) Traer lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Buscar la columna
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var columna = lista.Plantilla
                               .Columnas
                               .SingleOrDefault(c => c.Id.Equals(colId));
            if (columna is null)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            // 3) Mapear respuesta
            return new Response(
                ColumnaNumero: req.ColumnaNumero,
                Nombre: columna.Nombre.Valor,
                Modo: columna.Modo.ToString(),
                EsBase: columna.EsBase,
                Visible: columna.Visible,
                Orden: columna.Orden
            );
        }
    }
}
