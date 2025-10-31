// src/SharedKernel/ValueObjects/CategoriaId.cs
using System;
using System.Diagnostics.CodeAnalysis;

namespace SharedKernel.ValueObjects // ← usa el MISMO namespace que EmpresaId.cs
{
    /// <summary>
    /// Id fuertemente tipado para Categoria. Evita Guid.Empty.
    /// </summary>
    public readonly record struct CategoriaId
    {
        public Guid Value { get; }

        private CategoriaId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("CategoriaId no puede ser Guid.Empty.", nameof(value));
            Value = value;
        }

        /// <summary>Crea un nuevo Id.</summary>
        public static CategoriaId New() => new CategoriaId(Guid.NewGuid());

        /// <summary>Construye desde Guid (valida no Empty).</summary>
        public static CategoriaId From(Guid value) => new CategoriaId(value);

        /// <summary>Construye desde string (valida formato y no Empty).</summary>
        public static CategoriaId FromString(string value)
        {
            if (!Guid.TryParse(value, out var guid))
                throw new FormatException("El valor proporcionado no es un GUID válido para CategoriaId.");
            return From(guid);
        }

        /// <summary>TryParse seguro (no lanza excepciones).</summary>
        public static bool TryParse([NotNullWhen(true)] string? value, out CategoriaId result)
        {
            result = default;
            if (!Guid.TryParse(value, out var guid) || guid == Guid.Empty) return false;
            result = new CategoriaId(guid);
            return true;
        }

        /// <summary>Devuelve el GUID subyacente como cadena.</summary>
        public override string ToString() => Value.ToString();

        // Conversiones explícitas (evitamos implícitas para no colar Guid.Empty).
        public static explicit operator Guid(CategoriaId id) => id.Value;
        public static explicit operator CategoriaId(Guid value) => From(value);
    }
}
