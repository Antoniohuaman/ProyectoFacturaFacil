using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Registra una nueva Serie para un tipo de comprobante en un Establecimiento existente.
    /// - Valida que el tenant exista.
    /// - Resuelve el Establecimiento por su código (tal como llega del formulario).
    /// - Valida formato/consistencia de la Serie y el Correlativo inicial.
    /// - Marca como “por defecto” si se solicita (el agregado desmarca la previa).
    /// </summary>
    public sealed class RegistrarSerieUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public RegistrarSerieUseCase(IConfiguracionEmpresaRepository repo, IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
        }

        // ----------------- Entrada -----------------
        public sealed record Params(
            Guid   TenantId,
            string TipoComprobante,        // "01" | "03" | "FACTURA" | "BOLETA" | "F"/"B"
            string Serie,                   // "F001" | "B001" (formato A999)
            string EstablecimientoCodigo,   // código del establecimiento seleccionado en el formulario
            string CorrelativoInicial,      // "1" (o "00000001") … máximo 8 dígitos
            string? TipoOperacion = null,   // null => 0101 (Venta interna)
            bool EsPorDefecto = false
        );

        // ----------------- Salida -----------------
        public sealed record Result(
            Guid   SerieId,
            string TipoComprobanteCodigo,
            string Serie,
            Guid   EstablecimientoId,
            int    Correlativo,
            string TipoOperacionCodigo,
            bool   EsPorDefecto
        );

        // ----------------- Ejecutar -----------------
        public async Task<Result> ExecuteAsync(Params p, CancellationToken ct = default)
        {
            if (p.TenantId == Guid.Empty)
                throw new ArgumentException("TenantId inválido.", nameof(p.TenantId));

            var agg = await _repo.GetByTenantIdAsync(p.TenantId, ct);
            if (agg is null)
                throw new InvalidOperationException("La configuración de empresa aún no existe para este Tenant.");

            // Resolver establecimiento
            var estRead = agg.BuscarEstablecimientoPorCodigo(p.EstablecimientoCodigo);
            if (estRead is null)
                throw new InvalidOperationException($"El establecimiento con código \"{p.EstablecimientoCodigo}\" no existe.");

            // VOs desde la entrada
            var tipo          = TipoComprobanteCodigo.From(p.TipoComprobante);             // "01"/"03"/alias
            var serie         = SerieCodigo.ForTipo(p.Serie, tipo);                        // valida prefijo según tipo
            var correlativo   = Correlativo.FromString(p.CorrelativoInicial);              // 1..99,999,999 (8 dígitos)
            var tipoOperacion = p.TipoOperacion is null ? null : TipoOperacion.From(p.TipoOperacion);

            // Agregar serie en el agregado
            var serieId = agg.AgregarSerie(
                tipo: tipo,
                serie: serie,
                establecimientoId: estRead.Id,
                correlativoInicial: correlativo,
                tipoOperacion: tipoOperacion,
                esPorDefecto: p.EsPorDefecto
            );

            // Persistir
            await _uow.SaveChangesAsync(ct);

            // Leer del propio agregado para devolver info consistente
            var creada = agg.ObtenerSeriePorId(serieId)!;

            return new Result(
                SerieId: creada.Id,
                TipoComprobanteCodigo: creada.Tipo.Codigo,
                Serie: creada.Serie,
                EstablecimientoId: creada.EstablecimientoId,
                Correlativo: creada.CorrelativoActual.Valor,
                TipoOperacionCodigo: creada.TipoOperacion.Codigo,
                EsPorDefecto: creada.EsPorDefecto
            );
        }
    }
}