using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.Interfaces;   // IUnitOfWork
using ConfiguracionSistemaBC.Domain.ValueObjects; // UnidadDeMedida
using SharedKernel.Application.Interfaces;         // ITenantContext
using SharedKernel.ValueObjects;                   // EmpresaId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Registra una o más unidades de medida PERSONALIZADAS para la empresa.
    /// Reglas:
    /// - El código siempre proviene del catálogo normativo (Sunat/UNECE).
    /// - Unicidad por código; el aggregate valida y lanza si ya existe.
    /// - Solo una puede venir marcada como "por defecto" en la petición.
    /// - No se puede marcar como default una unidad con Visible=false.
    /// - Las preconfiguradas del sistema no se tocan en este caso; aquí solo se crean personalizadas.
    /// </summary>
    public sealed class RegistrarUnidadDeMedidaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public RegistrarUnidadDeMedidaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<RegistrarUnidadDeMedidaOutputDto> HandleAsync(
            RegistrarUnidadDeMedidaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.Items is null || input.Items.Count == 0)
                throw new ArgumentException("Debes enviar al menos una unidad de medida.", nameof(input.Items));

            // Empresa destino (input o contexto)
            var empresaId = !string.IsNullOrWhiteSpace(input.EmpresaId)
                ? EmpresaId.From(input.EmpresaId!.Trim())
                : _tenant.EmpresaId ?? throw new InvalidOperationException("No hay EmpresaId en el contexto.");

            var empresa = await _repo.GetByEmpresaIdAsync(empresaId, ct)
                ?? throw new KeyNotFoundException("No se encontró la configuración de la empresa.");

            // Validaciones de conjunto
            var defaultsMarcados = input.Items.Count(i => i.EsPorDefecto);
            if (defaultsMarcados > 1)
                throw new InvalidOperationException("Solo puedes marcar una unidad de medida como 'por defecto'.");

            foreach (var i in input.Items)
            {
                if (string.IsNullOrWhiteSpace(i.UnidadCodigo))
                    throw new ArgumentNullException(nameof(i.UnidadCodigo), "El código de unidad es obligatorio.");
                if (string.IsNullOrWhiteSpace(i.Nombre))
                    throw new ArgumentNullException(nameof(i.Nombre), "El nombre visible es obligatorio.");
                if (i.EsPorDefecto && !i.Visible)
                    throw new InvalidOperationException("No puedes establecer por defecto una unidad de medida oculta.");
            }

            var versionOriginal = empresa.Version;
            var creadas = new List<RegistrarUnidadDeMedidaOutputDto.UnidadCreada>();

            // Altas
            foreach (var item in input.Items)
            {
                var unidad = MapUnidad(item.UnidadCodigo);

                var id = empresa.AgregarUnidadDeMedidaPersonalizada(
                    unidad,
                    item.Nombre.Trim(),
                    visible: item.Visible,
                    orden: item.Orden,
                    esPorDefecto: item.EsPorDefecto
                );

                var dto = empresa.ListarUnidadesDeMedida()
                                 .First(u => u.Id == id);

                creadas.Add(new RegistrarUnidadDeMedidaOutputDto.UnidadCreada
                {
                    Id = dto.Id,
                    UnidadCodigo = dto.Unidad.Codigo,
                    Nombre = dto.Nombre,
                    Visible = dto.Visible,
                    EsPorDefecto = dto.EsPorDefecto,
                    EsSistema = dto.EsSistema,
                    Orden = dto.Orden
                });
            }

            // Persistencia (concurrencia optimista)
            await _repo.UpdateAsync(empresa, ct);
            await _uow.CommitAsync(ct);

            var defaultActual = empresa.ObtenerUnidadDeMedidaPorDefecto();
            var total = empresa.ListarUnidadesDeMedida().Count;

            return new RegistrarUnidadDeMedidaOutputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                Creadas = creadas,
                UnidadDefaultId = defaultActual?.Id,
                TotalUnidades = total
            };
        }

        // -------------------- Helpers --------------------

        private static UnidadDeMedida MapUnidad(string codigo)
        {
            var c = (codigo ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(c)) throw new ArgumentNullException(nameof(codigo));
            // Tu VO soporta conversión/costructor por código (según tu aggregate).
            // En tu dominio se usa (UnidadDeMedida)"DZN" y new UnidadDeMedida("GRM", null);
            // Aquí preferimos la conversión directa por coherencia con el dominio.
            return (UnidadDeMedida)c;
        }
    }
}
