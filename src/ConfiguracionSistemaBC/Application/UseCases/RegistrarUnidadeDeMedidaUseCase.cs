using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.DTOs;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Registra una o varias unidades de medida personalizadas en la configuración de empresa.
    /// Reglas:
    /// - El código de la unidad es normativo (SUNAT/UNECE) y debe ser único en la empresa.
    /// - Puedes marcar una como "por defecto" (debe estar Visible=true).
    /// - No elimina ni altera las del sistema; sólo agrega las indicadas.
    /// </summary>
    public sealed class RegistrarUnidadDeMedidaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public RegistrarUnidadDeMedidaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<RegistrarUnidadDeMedidaOutputDto> ExecuteAsync(
            RegistrarUnidadDeMedidaInputDto input,
            CancellationToken ct = default)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.EmpresaId))
                throw new ArgumentException("EmpresaId obligatorio.", nameof(input.EmpresaId));
            if (input.Items == null || input.Items.Count == 0)
                throw new ArgumentException("Debes enviar al menos una unidad de medida.", nameof(input.Items));

            var empresaIdVO = new EmpresaId(input.EmpresaId);
            var agg = await _repo.GetByEmpresaIdAsync(empresaIdVO, ct)
                      ?? throw new InvalidOperationException("Configuración de empresa no encontrada.");

            var creadas = new List<RegistrarUnidadDeMedidaOutputDto.UnidadCreada>();

            foreach (var it in input.Items)
            {
                if (string.IsNullOrWhiteSpace(it.Codigo))
                    throw new ArgumentException("Código de unidad obligatorio.", nameof(it.Codigo));
                if (string.IsNullOrWhiteSpace(it.Nombre))
                    throw new ArgumentException("Nombre de unidad obligatorio.", nameof(it.Nombre));

                // Respeta el código SUNAT tal como lo envía la UI (normalizamos a mayúsculas por consistencia)
                var codigo = it.Codigo.Trim().ToUpperInvariant();
                var unidadVO = new UnidadDeMedida(codigo, null);

                var id = agg.AgregarUnidadDeMedidaPersonalizada(
                    unidad: unidadVO,
                    nombre: it.Nombre.Trim(),
                    visible: it.Visible ?? true,
                    orden: it.Orden,
                    esPorDefecto: it.EsPorDefecto ?? false
                );

                creadas.Add(new RegistrarUnidadDeMedidaOutputDto.UnidadCreada(
                    Id: id,
                    Codigo: codigo,
                    Nombre: it.Nombre.Trim(),
                    Visible: it.Visible ?? true,
                    EsPorDefecto: it.EsPorDefecto ?? false,
                    Orden: it.Orden
                ));
            }

            await _repo.UpdateAsync(agg, ct);
            await _uow.SaveChangesAsync(ct);

            var def = agg.ObtenerUnidadDeMedidaPorDefecto();

            return new RegistrarUnidadDeMedidaOutputDto(
                EmpresaId: input.EmpresaId,
                TotalAgregadas: creadas.Count,
                DefaultId: def?.Id,
                Creadas: creadas
            );
        }
    }
}
