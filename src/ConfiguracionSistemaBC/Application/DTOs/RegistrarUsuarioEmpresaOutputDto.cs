using System;
using System.Collections.Generic;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Application.DTOs
{
    /// <summary>
    /// Resultado del registro de usuario.
    /// </summary>
    public sealed class RegistrarUsuarioEmpresaOutputDto
    {
        public string EmpresaId { get; }
        public Guid UsuarioId { get; }
        public string Email { get; }
        public string Estado { get; }
        public int Version { get; }
        public IReadOnlyList<AccesoResult> Accesos { get; }

        public RegistrarUsuarioEmpresaOutputDto(
            string empresaId,
            Guid usuarioId,
            string email,
            string estado,
            int version,
            IReadOnlyList<AccesoResult> accesos)
        {
            EmpresaId = empresaId;
            UsuarioId = usuarioId;
            Email = email;
            Estado = estado;
            Version = version;
            Accesos = accesos;
        }

        public sealed class AccesoResult
        {
            public Guid EstablecimientoId { get; }
            public IReadOnlyList<Guid> RolIds { get; }

            public AccesoResult(Guid establecimientoId, IReadOnlyList<Guid> rolIds)
            {
                EstablecimientoId = establecimientoId;
                RolIds = rolIds;
            }
        }
    }
}
