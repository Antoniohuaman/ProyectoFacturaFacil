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
    /// Registra un nuevo Establecimiento en el agregado ConfiguracionEmpresa.
    /// Respeta las reglas del dominio (PE obligatorio, código único, etc.).
    /// </summary>
    public sealed class RegistrarEstablecimientoUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public RegistrarEstablecimientoUseCase(IConfiguracionEmpresaRepository repo, IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<DTOs.RegistrarEstablecimientoOutputDto> Handle(
            DTOs.RegistrarEstablecimientoInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.Ruc))
                throw new ArgumentException("RUC obligatorio.", nameof(input.Ruc));
            if (string.IsNullOrWhiteSpace(input.Codigo))
                throw new ArgumentException("El código del establecimiento es obligatorio.", nameof(input.Codigo));
            if (string.IsNullOrWhiteSpace(input.Nombre))
                throw new ArgumentException("El nombre del establecimiento es obligatorio.", nameof(input.Nombre));
            if (input.Direccion is null)
                throw new ArgumentNullException(nameof(input.Direccion), "La dirección del establecimiento es obligatoria.");

            // 1) Obtener agregado por RUC
            var ruc = Ruc.From(input.Ruc);
                var agg = await _repo.FindByRucAsync(ruc, ct).ConfigureAwait(false);
            if (agg is null)
                throw new KeyNotFoundException("No se encontró la configuración de empresa para el RUC especificado.");

            // 2) Registrar
            var nuevoId = agg.RegistrarEstablecimiento(
                codigo:  input.Codigo,
                nombre:  input.Nombre,
                direccion: input.Direccion,
                telefono: input.Telefono,
                email:    input.Email
            );

            // 3) Persistir
            // Si tu implementación requiere Update explícito:
            // _repo.Update(agg);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            // 4) Leer para salida (usamos el read model del aggregate)
            var read = agg.BuscarEstablecimientoPorCodigo(input.Codigo)
                       ?? throw new InvalidOperationException("No se pudo recuperar el establecimiento recién creado.");

            return new DTOs.RegistrarEstablecimientoOutputDto(
                Id: read.Id,
                EmpresaId: read.EmpresaId,
                Codigo: read.Codigo,
                Nombre: read.Nombre,
                Direccion: read.Direccion,
                Habilitado: read.Habilitado
            );
        }
    }
}
