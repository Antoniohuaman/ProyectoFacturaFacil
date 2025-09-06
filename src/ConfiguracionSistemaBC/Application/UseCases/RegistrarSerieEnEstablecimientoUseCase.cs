using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;          // SerieComprobante
using ConfiguracionSistemaBC.Domain.Repositories;        // ISerieComprobanteRepository, IConfiguracionEmpresaRepository, IUnitOfWork
using ConfiguracionSistemaBC.Domain.ValueObjects;        // TipoComprobanteCodigo, SerieCodigo, TipoOperacion, Correlativo
using SharedKernel.Application.Interfaces;               // ITenantContext
using SharedKernel.ValueObjects;                         // EmpresaId, EstablecimientoId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Registra una nueva serie en un establecimiento ya existente de la empresa actual.
    /// Valida unicidad (EmpresaId+Tipo+Serie), existencia del establecimiento y reglas básicas.
    /// </summary>
    public sealed class RegistrarSerieEnEstablecimientoUseCase
    {
        private readonly ISerieComprobanteRepository _seriesRepo;
        private readonly IConfiguracionEmpresaRepository _configRepo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public RegistrarSerieEnEstablecimientoUseCase(
            ISerieComprobanteRepository seriesRepo,
            IConfiguracionEmpresaRepository configRepo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _seriesRepo = seriesRepo ?? throw new ArgumentNullException(nameof(seriesRepo));
            _configRepo = configRepo ?? throw new ArgumentNullException(nameof(configRepo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<RegistrarSerieEnEstablecimientoOutputDto> HandleAsync(
            RegistrarSerieEnEstablecimientoInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.EstablecimientoId))
                throw new ArgumentNullException(nameof(input.EstablecimientoId));
            if (string.IsNullOrWhiteSpace(input.Serie))
                throw new ArgumentNullException(nameof(input.Serie));

            var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("No hay EmpresaId en el contexto actual.");
            var establecimientoId = EstablecimientoId.FromString(input.EstablecimientoId);

            // 1) Validar que el establecimiento pertenece a la empresa y existe
            var existeEst = await _configRepo.EstablecimientoExisteAsync(empresaId, establecimientoId, ct);
            if (!existeEst)
                throw new KeyNotFoundException("El establecimiento no existe en la empresa actual.");

            // 2) Mapear/validar VOs
            var tipo = TipoComprobanteCodigo.From(input.TipoComprobante);
            var serie = SerieCodigo.ForTipo(input.Serie, tipo); // valida prefijo contra el tipo
            var op = string.IsNullOrWhiteSpace(input.TipoOperacion)
                ? TipoOperacion.Default
                : TipoOperacion.From(input.TipoOperacion);

            var corr = string.IsNullOrWhiteSpace(input.CorrelativoInicial)
                ? Correlativo.From(1)
                : Correlativo.FromString(input.CorrelativoInicial);

            var esPorDefecto = input.EsPorDefecto ?? false;
            var habilitada = input.Habilitada ?? true;

            if (esPorDefecto && !habilitada)
                throw new InvalidOperationException("No se puede marcar por defecto una serie inhabilitada.");

            // 3) Unicidad natural (EmpresaId + Tipo + Serie)
            if (await _seriesRepo.ExistsByTipoSerieAsync(empresaId, tipo, serie, ct))
                throw new InvalidOperationException($"Ya existe la serie {serie} para el tipo {tipo} en esta empresa.");

            // 4) Si va a ser “por defecto”, desmarcar las existentes (optimización de infra)
            if (esPorDefecto)
                await _seriesRepo.UnsetDefaultForTipoAsync(empresaId, tipo, ct);

            // 5) Crear agregado
            var nuevaSerie = SerieComprobante.Crear(
                empresaId,
                tipo,
                serie,
                establecimientoId,
                op,
                corr,
                esPorDefecto,
                habilitada);

            // 6) Persistencia
            await _seriesRepo.AddAsync(nuevaSerie, ct);
            await _uow.SaveChangesAsync(ct);

            // 7) Salida
            return new RegistrarSerieEnEstablecimientoOutputDto
            {
                SerieComprobanteId = nuevaSerie.Id,
                EmpresaId = empresaId.Value,
                EstablecimientoId = establecimientoId.Value,
                TipoComprobante = nuevaSerie.Tipo.Codigo,
                Serie = nuevaSerie.Serie.Codigo,
                TipoOperacion = nuevaSerie.TipoOperacion.Codigo,
                SiguienteCorrelativo = nuevaSerie.Siguiente.Valor,
                EsPorDefecto = nuevaSerie.EsPorDefecto,
                Habilitada = nuevaSerie.Habilitada,
                Version = nuevaSerie.Version
            };
        }
    }
}
