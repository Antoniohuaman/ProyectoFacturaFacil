namespace CatalogoArticulosBC.Domain.Entities
{
    public class AtributoVariante
    {
        public string Nombre { get; private set; }
        public string Valor { get; private set; }

        public AtributoVariante(string nombre, string valor)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Nombre de atributo inválido.", nameof(nombre));
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("Valor de atributo inválido.", nameof(valor));

            Nombre = nombre;
            Valor = valor;
        }
    }
}
