using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Cambia el ambiente de la empresa (p. ej., PRUEBA -> PRODUCCION) validando la transición
    /// mediante las reglas del aggregate (AmbienteFe.ValidarTransicion).
    /// </summary>
    public sealed class CambiarAmbienteEmpresaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public CambiarAmbienteEmpresaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<DTOs.CambiarAmbienteEmpresaOutputDto> Handle(
            DTOs.CambiarAmbienteEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.Ruc))
                throw new ArgumentException("RUC obligatorio.", nameof(input.Ruc));

            // 1) Cargar aggregate por RUC
            var ruc = Ruc.From(input.Ruc);
                var agg = await _repo.FindByRucAsync(ruc, ct).ConfigureAwait(false);
            if (agg is null)
                throw new KeyNotFoundException("Configuración de empresa no encontrada para el RUC especificado.");

            // 2) Capturar ambiente anterior y cambiar
            var anterior = agg.Ambiente;
            agg.CambiarAmbiente(input.Destino); // valida por dominio

            // 3) Persistir
            // Si tu repo requiere marcar Update explícitamente, expón el método en la interfaz y úsalo aquí.
            // _repo.Update(agg);
                await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            // 4) Salida
            return new DTOs.CambiarAmbienteEmpresaOutputDto(
                EmpresaId: agg.EmpresaId.Value,
                Ruc: agg.Ruc,
                AmbienteAnterior: anterior,
                AmbienteActual: agg.Ambiente,
                Version: agg.Version
            );
        }
    }
}
