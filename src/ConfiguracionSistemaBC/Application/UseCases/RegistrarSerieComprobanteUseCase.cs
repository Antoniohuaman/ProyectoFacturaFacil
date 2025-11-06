using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;                 // SerieComprobante
using ConfiguracionSistemaBC.Domain.Repositories;               // ISerieComprobanteRepository, IConfiguracionEmpresaRepository
using ConfiguracionSistemaBC.Application.Interfaces;            // IUnitOfWork
using ConfiguracionSistemaBC.Domain.ValueObjects;               // TipoComprobanteCodigo, SerieCodigo, TipoOperacion, Correlativo
using SharedKernel.Application.Interfaces;                       // ITenantContext
using SharedKernel.ValueObjects;                                 // EmpresaId, EstablecimientoId

namespace ConfiguracionSistemaBC.Application.UseCases.Series
{
    /// <summary>
    /// Registra una nueva Serie de Comprobante para la empresa actual (multiempresa) y un establecimiento existente.
    /// Reglas aplicadas:
    /// - El Establecimiento debe existir en la empresa.
    /// - (EmpresaId, Tipo, Serie) debe ser único.
    /// - Si se marca como PorDefecto, se desmarca la anterior por ese tipo (exclusividad).
    /// - Prefijo de Serie debe corresponder al Tipo (F para 01, B para 03).
    /// - Correlativo inicial en rango [1..99,999,999].
    /// </summary>
    public sealed class RegistrarSerieComprobanteUseCase
    {
        private readonly ISerieComprobanteRepository _series;
        private readonly IConfiguracionEmpresaRepository _empresas;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public RegistrarSerieComprobanteUseCase(
            ISerieComprobanteRepository seriesRepository,
            IConfiguracionEmpresaRepository configuracionEmpresaRepository,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _series   = seriesRepository ?? throw new ArgumentNullException(nameof(seriesRepository));
            _empresas = configuracionEmpresaRepository ?? throw new ArgumentNullException(nameof(configuracionEmpresaRepository));
            _uow      = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant   = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<RegistrarSerieComprobanteOutputDto> HandleAsync(
            RegistrarSerieComprobanteInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.TipoComprobante))
                throw new ArgumentNullException(nameof(input.TipoComprobante));
            if (string.IsNullOrWhiteSpace(input.Serie))
                throw new ArgumentNullException(nameof(input.Serie));
            if (string.IsNullOrWhiteSpace(input.EstablecimientoId))
                throw new ArgumentNullException(nameof(input.EstablecimientoId));

            var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId no disponible en el contexto.");

            // Mapear DTO → VOs (respetando tus tipos del Dominio/SharedKernel)
            var tipo = TipoComprobanteCodigo.From(input.TipoComprobante);
            var serie = SerieCodigo.ForTipo(input.Serie, tipo); // valida prefijo contra tipo
            var estId = EstablecimientoId.FromString(input.EstablecimientoId);
            var correlativoInicial = Correlativo.From(input.CorrelativoInicial);

            var tipoOperacion =
                string.IsNullOrWhiteSpace(input.TipoOperacion)
                    ? (TipoOperacion?)null
                    : TipoOperacion.From(input.TipoOperacion!);

            // Regla simple de coherencia para UI: no se puede marcar default si está inhabilitada
            if (input.EsPorDefecto && !input.Habilitada)
                throw new InvalidOperationException("No se puede marcar por defecto una serie inhabilitada.");

            // Verificar que el establecimiento pertenezca a la empresa
            var existeEst = await _empresas.EstablecimientoExisteAsync(empresaId, estId, ct);
            if (!existeEst)
                throw new InvalidOperationException("El establecimiento no existe en esta empresa.");

            // Unicidad (EmpresaId, Tipo, Serie)
            var yaExiste = await _series.ExistsByTipoSerieAsync(empresaId, tipo, serie, ct);
            if (yaExiste)
                throw new InvalidOperationException($"Ya existe una serie \"{serie}\" para el tipo {tipo.Codigo} en esta empresa.");

            // Si se pide por defecto, limpiamos la anterior (exclusividad)
            if (input.EsPorDefecto)
                await _series.UnsetDefaultForTipoAsync(empresaId, tipo, ct);

            // Crear aggregate
            var agregado = SerieComprobante.Crear(
                empresaId,
                tipo,
                serie,
                estId,
                tipoOperacion,
                correlativoInicial,
                esPorDefecto: input.EsPorDefecto,
                habilitada: input.Habilitada);

            // Persistir
            await _series.AddAsync(agregado, ct);
            await _uow.CommitAsync(ct);

            // Salida
            return new RegistrarSerieComprobanteOutputDto
            {
                Id = agregado.Id,
                EmpresaId = empresaId.Value,
                TipoComprobante = agregado.Tipo.Codigo,
                Serie = agregado.Serie.Codigo,
                EstablecimientoId = agregado.EstablecimientoId.Value.ToString("D"),
                TipoOperacion = agregado.TipoOperacion.Codigo,
                CorrelativoInicial = agregado.Siguiente.Valor,
                EsPorDefecto = agregado.EsPorDefecto,
                Habilitada = agregado.Habilitada,
                Version = agregado.Version
            };
        }
    }
}
