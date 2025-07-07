namespace CatalogoArticulosBC.Domain.Entities
{
    public class MultimediaProducto
    {
        public Guid Id { get; private set; }
        public string Url { get; private set; }
        public string Tipo { get; private set; }

        public MultimediaProducto(Guid id, string url, string tipo)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL inválida.", nameof(url));
            if (string.IsNullOrWhiteSpace(tipo))
                throw new ArgumentException("Tipo inválido.", nameof(tipo));

            Id = id;
            Url = url;
            Tipo = tipo;
        }
    }
}
