namespace ListaPreciosBC.Domain.ValueObjects
{
    public class Prioridad
    {
        public string? Nombre { get; }

        public Prioridad(string? nombre)
        {
            if (nombre != null && nombre != "Alta" && nombre != "Media" && nombre != "Baja" && nombre != "Sin definir")
                throw new ArgumentException("Prioridad debe ser 'Alta', 'Media', 'Baja', 'Sin definir' o nula.");
            Nombre = nombre;
        }

        public static Prioridad Alta => new Prioridad("Alta");
        public static Prioridad Media => new Prioridad("Media");
        public static Prioridad Baja => new Prioridad("Baja");
        public static Prioridad SinDefinir => new Prioridad("Sin definir");
        public static Prioridad Ninguna => new Prioridad(null);

        public override string ToString() => Nombre ?? "Sin prioridad";
    }
}