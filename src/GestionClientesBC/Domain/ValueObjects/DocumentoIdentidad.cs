using System;
using System.Linq;

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// VO que representa el documento de identidad.
    /// </summary>
    public sealed record DocumentoIdentidad
    {
        public TipoDocumento Tipo { get; }
        public string Numero { get; }

        public DocumentoIdentidad(TipoDocumento tipo, string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentNullException(nameof(numero));

            // Ejemplo: solo números y 8 dígitos para DNI
            if (tipo == TipoDocumento.DNI && (!numero.All(char.IsDigit) || numero.Length != 8))
                throw new ArgumentException("El DNI debe tener 8 dígitos numéricos.", nameof(numero));

            // Puedes agregar validaciones para otros tipos aquí (RUC, etc.)

            Tipo = tipo;
            Numero = numero;
        }

        public override string ToString() => $"{Tipo}-{Numero}";
    }
}