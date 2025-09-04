using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases.Dtos;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso para registrar una nueva serie en un establecimiento
    /// de una empresa, respetando las reglas del agregado.
    /// </summary>
    public sealed class RegistrarSerieUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public RegistrarSerieUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<RegistrarSerieOutputDto> Handle(RegistrarSerieInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.Ruc)) throw new ArgumentNullException(nameof(input.Ruc));
            if (string.IsNullOrWhiteSpace(input.TipoComprobanteCodigo)) throw new ArgumentNullException(nameof(input.TipoComprobanteCodigo));
            if (string.IsNullOrWhiteSpace(input.Serie)) throw new ArgumentNullException(nameof(input.Serie));
            if (input.CorrelativoInicial <= 0) throw new ArgumentOutOfRangeException(nameof(input.CorrelativoInicial), "El correlativo inicial debe ser mayor que cero.");

            // 1) cargar agregado por RUC
            var ruc = Ruc.From(input.Ruc);
            var empresa = await _repo.FindByRucAsync(ruc, ct).ConfigureAwait(false);
            if (empresa is null)
                throw new InvalidOperationException($"No existe configuración para el RUC {input.Ruc}.");

            // 2) mapear VOs a partir de los códigos
            var tipo = MapTipoComprobante(input.TipoComprobanteCodigo);
            var serieCodigo = SerieCodigo.From(input.Serie);
            var correlativo = Correlativo.From(input.CorrelativoInicial);
            var tipoOperacion = MapTipoOperacionOrNull(input.TipoOperacionCodigo);

            // 3) invocar lógica del agregado
            var nuevaSerieId = empresa.AgregarSerie(
                tipo,
                serieCodigo,
                input.EstablecimientoId,
                correlativo,
                tipoOperacion,
                esPorDefecto: input.EsPorDefecto
            );

            // 4) persistir cambios
            await _repo.UpdateAsync(empresa, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            // 5) armar output (usando read model del agregado para devolver estado real)
            var read = empresa.ObtenerSeriePorId(nuevaSerieId)
                       ?? throw new InvalidOperationException("No se pudo recuperar la serie recién creada.");

            return new RegistrarSerieOutputDto
            {
                SerieId = read.Id,
                EmpresaId = read.EmpresaId,
                EstablecimientoId = read.EstablecimientoId,
                TipoComprobanteCodigo = read.Tipo.Codigo,
                Serie = read.Serie,
                Correlativo = read.CorrelativoActual.Valor,
                TipoOperacionCodigo = read.TipoOperacion.Codigo,
                EsPorDefecto = read.EsPorDefecto,
                Bloqueada = read.Bloqueada,
                Version = empresa.Version
            };
        }

        // ===== Helpers (sin inventar términos; solo crean VOs desde códigos) =====

        private static TipoComprobanteCodigo MapTipoComprobante(string codigo)
        {
            // Si tu VO expone un From(codigo) úsalo; de lo contrario, resolvemos por los conocidos.
            try { return TipoComprobanteCodigo.From(codigo); }
            catch
            {
                // Fallback a los comunes:
                return codigo switch
                {
                    "01" => TipoComprobanteCodigo.Factura,
                    "03" => TipoComprobanteCodigo.Boleta,
                    _    => throw new ArgumentException($"Tipo de comprobante no soportado: {codigo}", nameof(codigo))
                };
            }
        }

        private static TipoOperacion? MapTipoOperacionOrNull(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null; // el agregado usará Default
            try { return TipoOperacion.From(codigo!.Trim()); }
            catch { return TipoOperacion.Default; } // fallback seguro a Default
        }
    }
}
