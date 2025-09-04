using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.DTOs
{
    public sealed class RegistrarFormasDePagoOutputDto
    {
        public string EmpresaId { get; }
        public int TotalAgregadas { get; }
        public Guid? DefaultId { get; }

        public IReadOnlyList<FormaPagoCreada> Creadas { get; }

        public RegistrarFormasDePagoOutputDto(
            string EmpresaId,
            int TotalAgregadas,
            Guid? DefaultId,
            IReadOnlyList<FormaPagoCreada> Creadas)
        {
            this.EmpresaId = EmpresaId;
            this.TotalAgregadas = TotalAgregadas;
            this.DefaultId = DefaultId;
            this.Creadas = Creadas;
        }

        public sealed record FormaPagoCreada(
            Guid Id,
            string PaymentMeansCode,
            string? MetodoCodigo,
            string? MetodoNombre,
            string Nombre,
            bool Visible,
            bool esPorDefecto,
            int? orden
        );
    }
}
