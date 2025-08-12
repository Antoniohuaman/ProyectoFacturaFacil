using System;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object de Establecimiento dentro del tenant (empresa).
    /// Igualdad por valor: (EmpresaId, EstablecimientoId, Nombre normalizado).
    /// Pensado para usarse en filtros/segmentos de KPIs; NO trae datos del BC de configuración.
    /// 
    /// Invariantes:
    ///  - EmpresaId != Guid.Empty
    ///  - EstablecimientoId != Guid.Empty
    ///  - Nombre opcional; si viene, se normaliza (Trim) y se valida longitud.
    /// 
    /// Notas:
    ///  - Para representar "todos los establecimientos" usa null en SegmentoIndicador.EstabelecimientoId
    ///    (no se modela un VO especial para "todos").
    /// </summary>
    public sealed record Establecimiento
    {
        private const int LongitudMaxNombre = 120;

        /// <summary>Identidad de la empresa (tenant) a la que pertenece el establecimiento.</summary>
        public Guid EmpresaId { get; }

        /// <summary>Identidad del establecimiento dentro de la empresa.</summary>
        public Guid EstablecimientoId { get; }

        /// <summary>Nombre descriptivo (opcional). Solo se usa para mostrar.</summary>
        public string? Nombre { get; }

        private Establecimiento(Guid empresaId, Guid establecimientoId, string? nombreNormalizado)
        {
            if (empresaId == Guid.Empty)
                throw new ArgumentException("EmpresaId no puede ser vacío.", nameof(empresaId));

            if (establecimientoId == Guid.Empty)
                throw new ArgumentException("EstablecimientoId no puede ser vacío.", nameof(establecimientoId));

            EmpresaId = empresaId;
            EstablecimientoId = establecimientoId;
            Nombre = nombreNormalizado;
        }

        /// <summary>
        /// Fábrica principal. Si <paramref name="nombre"/> es nulo o en blanco, se almacena como null.
        /// </summary>
        public static Establecimiento Crear(Guid empresaId, Guid establecimientoId, string? nombre = null)
            => new(empresaId, establecimientoId, NormalizarNombre(nombre));

        /// <summary>
        /// Devuelve una nueva instancia con el nombre actualizado (inmutable).
        /// </summary>
        public Establecimiento ConNombre(string? nuevoNombre)
            => new(EmpresaId, EstablecimientoId, NormalizarNombre(nuevoNombre));

        public bool TieneNombre => !string.IsNullOrWhiteSpace(Nombre);

        public override string ToString() => TieneNombre ? Nombre! : EstablecimientoId.ToString("D");

        // ----------------- helpers -----------------
        private static string? NormalizarNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return null;

            var trimmed = nombre.Trim();

            if (trimmed.Length > LongitudMaxNombre)
                throw new ArgumentException($"El nombre excede {LongitudMaxNombre} caracteres.", nameof(nombre));

            return trimmed;
        }
    }
}