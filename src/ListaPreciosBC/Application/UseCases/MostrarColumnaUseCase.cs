using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio
using SharedKernel.Exceptions;               // NotFoundException
using SharedKernel.Application.Interfaces;   // ITenantContext

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
            byte Orden,
            string TipoColumnaCodigo,
            string? TipoReglaGlobalCodigo,
            decimal? ValorReglaGlobal
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly ITenantContext _tenant;

        public MostrarColumnaUseCase(IListaPrecioRepository listaRepo, ITenantContext tenant)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
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

            // 2) Buscar la columna
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var columna = lista.Plantilla
                               .Columnas
                               .SingleOrDefault(c => c.Id.Equals(colId));
            if (columna is null)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            // 3) Mapear respuesta
            var regla = ColumnaPrecioApplicationMapper.ExtraerRegla(columna.ReglaGlobal);

            return new Response(
                ColumnaNumero: req.ColumnaNumero,
                Nombre: columna.Nombre.Valor,
                Modo: columna.Modo.ToString(),
                EsBase: columna.EsBase,
                Visible: columna.Visible,
                Orden: columna.Orden,
                TipoColumnaCodigo: columna.Tipo.Codigo,
                TipoReglaGlobalCodigo: regla.TipoCodigo,
                ValorReglaGlobal: regla.Valor
            );
        }
    }
}
