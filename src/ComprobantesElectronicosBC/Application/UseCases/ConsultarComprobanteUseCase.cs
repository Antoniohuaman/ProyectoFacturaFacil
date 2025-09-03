using System;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Application.UseCases.ConsultarComprobante
{
    /// <summary>
    /// Puerto de mapeo desde el agregado a un DTO “plano” apropiado para UI/API.
    /// Implementación en Adapters para no acoplar Application con la capa de dominio/infra.
    /// </summary>
    public interface IConsultarComprobanteMapper
    {
        ConsultarComprobanteOutputDto Map(ComprobanteElectronico aggregate);
    }

    /// <summary>
    /// Caso de uso: consultar un comprobante por Id o por Serie–Número.
    /// </summary>
    public sealed class ConsultarComprobanteUseCase
    {
        private readonly IComprobanteRepository _repo;
        private readonly IConsultarComprobanteMapper _mapper;

        public ConsultarComprobanteUseCase(
            IComprobanteRepository repo,
            IConsultarComprobanteMapper mapper)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Ejecuta la consulta. Prioriza Id cuando se pasan ambos criterios.
        /// Lanza <see cref="NotFoundException"/> si no existe.
        /// </summary>
        public async Task<ConsultarComprobanteOutputDto> ExecuteAsync(
            ConsultarComprobanteInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Preferir búsqueda por Id
            if (input.EsBusquedaPorId)
            {
                var agg = await _repo.GetByIdAsync(input.ComprobanteId!.Value, ct);
                if (agg is null)
                    throw NotFoundException.For<ComprobanteElectronico>(input.ComprobanteId);

                return _mapper.Map(agg);
            }

            // 2) Si no hay Id, permitir Serie–Número (validado con VO)
            if (input.EsBusquedaPorSerieNumero)
            {
                // Valida y normaliza usando tu VO (reglas: serie 1..4 A-Z0-9, número 1..99’999’999)
                var syN = SerieYNumero.Create(input.Serie!.Trim(), input.Numero!.Value);

                var agg = await _repo.GetBySerieNumeroAsync(syN.Serie, syN.Numero, ct);
                if (agg is null)
                    throw new NotFoundException(
                        resource: nameof(ComprobanteElectronico),
                        resourceId: syN.IdUbl);

                return _mapper.Map(agg);
            }

            // 3) Sin criterios válidos
            throw new ArgumentException("Proporcione ComprobanteId o Serie y Número para la consulta.", nameof(input));
        }
    }
}
