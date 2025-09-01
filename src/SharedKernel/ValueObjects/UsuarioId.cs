using System;
using System.Diagnostics;
using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Identidad global del usuario (transversal a todos los BCs).
    /// Evita usar GUIDs crudos y centraliza validación/creación.
    ///
    /// ADVERTENCIA: No utilice UsuarioId.Empty en lógica de dominio. Solo debe usarse para inicialización, serialización o pruebas.
    /// En la lógica de negocio, siempre debe existir un UsuarioId válido y no vacío.
    /// </summary>
    [DebuggerDisplay("{Value}")]
    public sealed record UsuarioId
    {
        /// <summary>GUID subyacente.</summary>
        public Guid Value { get; init; }

        private UsuarioId(Guid value) => Value = value;

        /// <summary>Crea un nuevo UsuarioId con Guid aleatorio.</summary>
        public static UsuarioId New() => new UsuarioId(Guid.NewGuid());

        /// <summary>Crea un UsuarioId validando que no sea Guid.Empty.</summary>
        public static UsuarioId From(Guid value)
        {
            if (value == Guid.Empty)
                throw new BusinessRuleException("UsuarioId no puede ser Guid.Empty.");
            return new UsuarioId(value);
        }

        /// <summary>Intento de creación sin lanzar excepción.</summary>
        public static bool TryFrom(Guid value, out UsuarioId usuarioId)
        {
            if (value == Guid.Empty)
            {
                usuarioId = UsuarioId.Empty;
                return false;
            }
            usuarioId = new UsuarioId(value);
            return true;
        }

        /// <summary>Crea desde string validando formato y que no sea vacío.</summary>
        public static UsuarioId FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new BusinessRuleException("UsuarioId no puede ser nulo o vacío.");

            if (!Guid.TryParse(value, out var guid) || guid == Guid.Empty)
                throw new BusinessRuleException("Formato inválido para UsuarioId.");

            return new UsuarioId(guid);
        }

        /// <summary>Parse no-excepcional desde string.</summary>
        public static bool TryParse(string? value, out UsuarioId usuarioId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                usuarioId = UsuarioId.Empty;
                return false;
            }
            if (!Guid.TryParse(value, out var guid) || guid == Guid.Empty)
            {
                usuarioId = UsuarioId.Empty;
                return false;
            }
            usuarioId = new UsuarioId(guid);
            return true;
        }

        /// <summary>Indica si el valor subyacente es Guid.Empty.</summary>
        public bool IsEmpty => Value == Guid.Empty;

        public override string ToString() => Value.ToString();

        // Conversiones explícitas útiles y seguras.
        public static explicit operator Guid(UsuarioId id) => id.Value;
        public static explicit operator UsuarioId(Guid value) => From(value);

        /// <summary>
    /// Valor por defecto (principalmente para infra/serialización).
    /// ADVERTENCIA: No utilice UsuarioId.Empty en lógica de dominio. Prefiera siempre From/New para obtener un UsuarioId válido.
        /// </summary>
        public static UsuarioId Empty => new UsuarioId(Guid.Empty);
    }
}
