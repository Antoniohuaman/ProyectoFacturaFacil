using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;


namespace CatalogoArticulosBC.Application.DTOs
{
    public sealed class CrearProductoSimpleDto
    {
        public CrearProductoSimpleDto(
            string sku,
            string nombre,
            string descripcion,
            string unidadMedida,
            string afectacionIgv,
            string codigoSunat,
            decimal baseImponibleVentas,
            string centroCosto,
            decimal presupuesto,
            decimal peso,
            TipoProducto tipo,
            decimal precio,
            string categoria,           // Obligatorio
            string? marca = null,        // Opcional
            Guid? imagenPrincipalId = null, // Opcional

            // NUEVOS CAMPOS COMPLEMENTARIOS
            decimal precioVentaSinIGV = 0,
            decimal precioVentaConIGV = 0,
            bool esAfectoICBPER = false,
            bool tieneDetraccion = false,
            string? codigoDetraccion = null,
            string? codigoBarras = null,
            string? codigoFabrica = null,
            string? codigoLote = null,
            string? serie = null,
            List<Guid>? almacenesAsignados = null,
            bool asignarATodosLosAlmacenes = false,
            Moneda moneda = Moneda.Soles,
            DateTime? fechaVencimiento = null,
            TipoExistencia tipoExistencia = TipoExistencia.Mercaderias
        )
        {
            if (string.IsNullOrWhiteSpace(categoria))
                throw new ArgumentException("La categoría es obligatoria.", nameof(categoria));

            Sku = sku;
            Nombre = nombre;
            Descripcion = descripcion;
            UnidadMedida = unidadMedida;
            AfectacionIgv = afectacionIgv;
            CodigoSunat = codigoSunat;
            BaseImponibleVentas = baseImponibleVentas;
            CentroCosto = centroCosto;
            Presupuesto = presupuesto;
            Peso = peso;
            Tipo = tipo;
            Precio = precio;
            Categoria = categoria;
            Marca = marca;
            ImagenPrincipalId = imagenPrincipalId;

            // Nuevos campos
            PrecioVentaSinIGV = precioVentaSinIGV;
            PrecioVentaConIGV = precioVentaConIGV;
            EsAfectoICBPER = esAfectoICBPER;
            TieneDetraccion = tieneDetraccion;
            CodigoDetraccion = codigoDetraccion;
            CodigoBarras = codigoBarras;
            CodigoFabrica = codigoFabrica;
            CodigoLote = codigoLote;
            Serie = serie;
            AlmacenesAsignados = almacenesAsignados ?? new List<Guid>();
            AsignarATodosLosAlmacenes = asignarATodosLosAlmacenes;
            Moneda = moneda;
            FechaVencimiento = fechaVencimiento;
            TipoExistencia = tipoExistencia;
        }

        public string Sku { get; set; }
        public string Nombre { get; }
        public string Descripcion { get; }
        public string UnidadMedida { get; }
        public string AfectacionIgv { get; }
        public string CodigoSunat { get; }
        public decimal BaseImponibleVentas { get; }
        public string CentroCosto { get; }
        public decimal Presupuesto { get; }
        public decimal Peso { get; }
        public TipoProducto Tipo { get; }
        public decimal Precio { get; set; }
        public string Categoria { get; } // Obligatorio
        public string? Marca { get; }    // Opcional
        public Guid? ImagenPrincipalId { get; } // Opcional

        // NUEVOS CAMPOS COMPLEMENTARIOS
        public decimal PrecioVentaSinIGV { get; }
        public decimal PrecioVentaConIGV { get; }
        public bool EsAfectoICBPER { get; }
        public bool TieneDetraccion { get; }
        public string? CodigoDetraccion { get; }
        public string? CodigoBarras { get; }
        public string? CodigoFabrica { get; }
        public string? CodigoLote { get; }
        public string? Serie { get; }
        public List<Guid> AlmacenesAsignados { get; }
        public bool AsignarATodosLosAlmacenes { get; }
        public Moneda Moneda { get; }
        public DateTime? FechaVencimiento { get; }
        public TipoExistencia TipoExistencia { get; }
    }
}