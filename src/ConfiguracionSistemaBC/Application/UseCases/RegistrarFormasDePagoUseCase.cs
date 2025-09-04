using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.DTOs;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Registra N formas de pago personalizadas sobre la configuración de empresa.
    /// - Respeta lo normativo SUNAT: PaymentMeansCode "10" (Contado) / "20" (Crédito).
    /// - Para "10" (Contado) puedes enviar MetodoCodigo (p.ej., EFECTIVO, TRANSFERENCIA, YAPE, ...).
    /// - Para "20" (Crédito) MetodoCodigo debe ir null (el método no aplica).
    /// - Permite marcar una de las enviadas como por defecto (debe estar Visible=true).
    /// - No altera ni quita lo ya configurado; solo agrega lo indicado.
    /// </summary>
    public sealed class RegistrarFormasDePagoUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public RegistrarFormasDePagoUseCase(IConfiguracionEmpresaRepository repo, IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<RegistrarFormasDePagoOutputDto> ExecuteAsync(
            RegistrarFormasDePagoInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.EmpresaId))
                throw new ArgumentException("EmpresaId obligatorio.", nameof(input.EmpresaId));
            if (input.Items is null || input.Items.Count == 0)
                throw new ArgumentException("Debes enviar al menos una forma de pago.", nameof(input.Items));

            var agg = await _repo.GetByEmpresaIdAsync(EmpresaId.From(input.EmpresaId), ct)
                      ?? throw new InvalidOperationException("Configuración de empresa no encontrada.");

            // Registrar cada forma de pago
            var creadas = new List<RegistrarFormasDePagoOutputDto.FormaPagoCreada>();
            foreach (var it in input.Items)
            {
                var vo = ToFormaDePagoVO(it);

                var id = agg.AgregarFormaDePagoPersonalizada(
                    vo,
                    nombre: it.Nombre?.Trim() ?? throw new ArgumentNullException(nameof(it.Nombre)),
                    visible: it.Visible ?? true,
                    orden: it.Orden,
                    esPorDefecto: it.EsPorDefecto ?? false
                );

                creadas.Add(new RegistrarFormasDePagoOutputDto.FormaPagoCreada(
                    id,
                    vo.PaymentMeansCode,
                    vo.MetodoCodigo,
                    vo.MetodoNombre,
                    it.Nombre!.Trim(),
                    it.Visible ?? true,
                    esPorDefecto: it.EsPorDefecto ?? false,
                    orden: it.Orden
                ));
            }

            await _repo.UpdateAsync(agg, ct);
            await _uow.SaveChangesAsync(ct);

            // Leer estado final (por si alguna marcada como default quedó efectivamente como tal)
            var defaultFp = agg.ObtenerFormaDePagoPorDefecto();

            return new RegistrarFormasDePagoOutputDto(
                EmpresaId: input.EmpresaId,
                TotalAgregadas: creadas.Count,
                DefaultId: defaultFp?.Id,
                Creadas: creadas
            );
        }

        private static FormaDePago ToFormaDePagoVO(RegistrarFormasDePagoInputDto.FormaDePagoItem it)
        {
            if (string.IsNullOrWhiteSpace(it.PaymentMeansCode))
                throw new ArgumentException("PaymentMeansCode obligatorio (\"10\"/\"20\").");

            var code = it.PaymentMeansCode.Trim();
            switch (code)
            {
                case FormaDePago.CONTADO: // "10"
                    // Si no vino método, usa el atajo Contado() -> método "CONTADO"
                    if (string.IsNullOrWhiteSpace(it.MetodoCodigo))
                        return FormaDePago.Contado();

                    // Normalizamos a MAYÚSCULAS; el VO Validará longitudes.
                    var met = it.MetodoCodigo!.Trim().ToUpperInvariant();
                    // Permitimos cualquier método (predefinido o personalizado).
                    return FormaDePago.ContadoPersonalizado(met, it.MetodoNombre);

                case FormaDePago.CREDITO: // "20"
                    // En crédito no aplica método.
                    return FormaDePago.Credito();

                default:
                    throw new ArgumentException("PaymentMeansCode inválido. Use \"10\" (Contado) o \"20\" (Crédito).");
            }
        }
    }
}
