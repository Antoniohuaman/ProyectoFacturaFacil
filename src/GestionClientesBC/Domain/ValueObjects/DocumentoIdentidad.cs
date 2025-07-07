using System;

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
            Tipo = tipo;
            Numero = numero ?? throw new ArgumentNullException(nameof(numero));
            // Aquí puedes agregar validaciones de formato según el tipo
        }

        public override string ToString() => $"{Tipo}-{Numero}";
    }
}