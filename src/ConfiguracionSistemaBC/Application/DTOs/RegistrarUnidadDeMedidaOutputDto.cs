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
        public List<UnidadCreada> Creadas { get; init; } = new();
        public Guid? UnidadDefaultId { get; init; }
        public int TotalUnidades { get; init; }

        public sealed class UnidadCreada
        {
            public Guid Id { get; init; }
            public string UnidadCodigo { get; init; } = string.Empty;
            public string Nombre { get; init; } = string.Empty;
            public bool Visible { get; init; }
            public bool EsPorDefecto { get; init; }
            public bool EsSistema { get; init; }
            public int Orden { get; init; }
        }
    }
}
