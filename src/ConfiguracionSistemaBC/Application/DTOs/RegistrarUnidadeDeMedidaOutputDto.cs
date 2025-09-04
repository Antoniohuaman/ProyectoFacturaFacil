using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.DTOs
{
    public sealed class RegistrarUnidadDeMedidaOutputDto
    {
        public string EmpresaId { get; }
        public int TotalAgregadas { get; }
        public Guid? DefaultId { get; }
        public IReadOnlyList<UnidadCreada> Creadas { get; }

        public RegistrarUnidadDeMedidaOutputDto(
            string EmpresaId,
            int TotalAgregadas,
            Guid? DefaultId,
            IReadOnlyList<UnidadCreada> Creadas)
        {
            this.EmpresaId = EmpresaId;
            this.TotalAgregadas = TotalAgregadas;
            this.DefaultId = DefaultId;
            this.Creadas = Creadas;
        }

        public sealed record UnidadCreada(
            Guid Id,
            string Codigo,
            string Nombre,
            bool Visible,
            bool EsPorDefecto,
            int? Orden
        );
    }
}
