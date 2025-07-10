using System;
using System.Linq;
namespace CatalogoArticulosBC.Domain.Entities
{
    public sealed class AtributoVariante
    {
        public string Nombre  { get; }
        public string Valor   { get; }

        public AtributoVariante(string nombre, string valor)
        {
            Nombre = !string.IsNullOrWhiteSpace(nombre)
                ? nombre
                : throw new ArgumentException("El nombre no puede estar vacío.", nameof(nombre));
            Valor  = valor ?? throw new ArgumentNullException(nameof(valor));
        }
    }
}