using System;
using System.Text.RegularExpressions;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Centro de costo (VO compartido). Identidad: Code + Name.
    /// </summary>
    public readonly record struct CentroDeCosto
    {
        public const int MaxCodeLength = 35;
        public const int MaxNameLength = 100;

        private static readonly Regex CodePattern =
            new(@"^[A-Z0-9\-_.\/ ]+$", RegexOptions.Compiled);

        public string Code { get; }
        public string? Name { get; }

        public CentroDeCosto(string code, string? name = null)
        {
            // Ambos campos son obligatorios: Código y Nombre
            // Código: no nulo, no vacío, máximo 35 caracteres, solo caracteres permitidos
            // Nombre: no nulo, no vacío, máximo 100 caracteres
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("El código de centro de costo es obligatorio.", nameof(code));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("La descripción (nombre) del centro de costo es obligatoria.", nameof(name));

            code = code.Trim().ToUpperInvariant();
            if (code.Length > MaxCodeLength)
                throw new ArgumentException($"El código no puede exceder {MaxCodeLength} caracteres.", nameof(code));
            if (!CodePattern.IsMatch(code))
                throw new ArgumentException("Código con caracteres no permitidos. Permitidos: A-Z, 0-9, espacio, -, _, ., /", nameof(code));

            name = name.Trim();
            if (name.Length > MaxNameLength)
                throw new ArgumentException($"El nombre no puede exceder {MaxNameLength} caracteres.", nameof(name));

            Code = code;
            Name = name;
        }

        public static CentroDeCosto Create(string code, string? name = null) => new(code, name);

        public static CentroDeCosto? FromOptional(string? code, string? name = null)
            => string.IsNullOrWhiteSpace(code) ? null : new CentroDeCosto(code!, name);

        public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Code : $"{Code} - {Name}";
        // Métodos requeridos por los tests
        public string ForUbl_AccountingCostCode() => Code;
        public string ForUbl_AccountingCost() => string.IsNullOrWhiteSpace(Name) ? Code : Name;
    }
}