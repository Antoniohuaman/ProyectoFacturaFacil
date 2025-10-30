using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using SharedKernel.Exceptions;               // NotFoundException
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Exporta la PLANTILLA (todas las columnas) de la Lista de Precios ACTIVA a CSV (compatible con Excel).
    /// Columnas del CSV: ColumnaNumero;Nombre;Modo;EsBase;Visible;Orden
    /// </summary>
    public sealed class ExportarPlantillaExcelUseCase
    {
        public readonly record struct Request(
            bool SoloVisibles = false
        );

        public readonly record struct Response(
            string NombreArchivo,
            string ContentType,
            byte[] Contenido,
            int ColumnasIncluidas,
            int VersionLista
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly ITenantContext _tenant;

        public ExportarPlantillaExcelUseCase(IListaPrecioRepository listaRepo, ITenantContext tenant)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            // 0) Contexto
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null) throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            // 1) Obtener lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Seleccionar columnas (filtrar y ordenar)
            var columnas = lista.Plantilla.Columnas
                                   .Where(c => !req.SoloVisibles || c.Visible)
                                   .OrderBy(c => c.Orden)
                                   .ToArray();

            // 3) Construir CSV
            var sb = new StringBuilder(capacity: 512);
            const char sep = ';';
            var ci = CultureInfo.InvariantCulture;

            // Header
            sb.Append("ColumnaNumero").Append(sep)
              .Append("Nombre").Append(sep)
              .Append("Modo").Append(sep)
              .Append("EsBase").Append(sep)
              .Append("Visible").Append(sep)
              .Append("Orden")
              .AppendLine();

            // Rows
            foreach (var col in columnas)
            {
                sb.Append(col.Id.Numero.ToString(ci)).Append(sep)
                  .Append(Escape(col.Nombre.Valor)).Append(sep)
                  .Append(col.Modo.ToString()).Append(sep)
                  .Append(col.EsBase.ToString(ci)).Append(sep)
                  .Append(col.Visible.ToString(ci)).Append(sep)
                  .Append(col.Orden.ToString(ci))
                  .AppendLine();
            }

            // 4) Empaquetar archivo
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"plantilla_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv";

            return new Response(
                NombreArchivo: fileName,
                ContentType: "text/csv; charset=utf-8",
                Contenido: bytes,
                ColumnasIncluidas: columnas.Length,
                VersionLista: lista.Version
            );
        }

        private static string Escape(string value)
        {
            // Si contiene separador o comillas, envolver en comillas dobles y duplicar comillas internas
            if (value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0)
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
