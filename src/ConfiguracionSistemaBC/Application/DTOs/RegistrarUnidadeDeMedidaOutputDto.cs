using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resultado de registrar unidades de medida personalizadas.
    /// </summary>
    public sealed class RegistrarUnidadDeMedidaOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;

        /// <summary>Unidades creadas en esta operación.</summary>
        public IReadOnlyList<UnidadCreada> Creadas { get; init; } = Array.Empty<UnidadCreada>();

        /// <summary>Id de la unidad por defecto vigente luego de la operación.</summary>
        public Guid? UnidadDefaultId { get; init; }

        /// <summary>Total de unidades (sistema + personalizadas) luego de la operación.</summary>
        public int TotalUnidades { get; init; }

        public sealed class UnidadCreada
        {
            public Guid Id { get; init; }
            public string UnidadCodigo { get; init; } = string.Empty;
            public string Nombre { get; init; } = string.Empty;
            public bool Visible { get; init; }
            public bool EsPorDefecto { get; init; }
            public bool EsSistema { get; init; } // siempre false para personalizadas
            public int Orden { get; init; }
        }
    }
}
