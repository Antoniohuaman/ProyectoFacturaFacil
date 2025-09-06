using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resultado del registro de formas de pago personalizadas.
    /// </summary>
    public sealed class RegistrarFormasDePagoOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;

        /// <summary>Listado de formas creadas en esta operación.</summary>
        public IReadOnlyList<FormaPagoCreada> Creadas { get; init; } = Array.Empty<FormaPagoCreada>();

        /// <summary>Id de la forma de pago por defecto vigente luego de la operación.</summary>
        public Guid? FormaPagoDefaultId { get; init; }

        /// <summary>Total de formas de pago (del sistema + personalizadas) luego de la operación.</summary>
        public int TotalFormasDePago { get; init; }

        public sealed class FormaPagoCreada
        {
            public Guid Id { get; init; }
            public string PaymentMeansCode { get; init; } = string.Empty; // "10"/"20"
            public string? MetodoCodigo { get; init; }                    // solo CONTADO (varía)
            public string Nombre { get; init; } = string.Empty;
            public bool Visible { get; init; }
            public bool EsPorDefecto { get; init; }
            public bool EsSistema { get; init; } // siempre false para personalizadas
            public int Orden { get; init; }
        }
    }
}
