using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.Specifications;
using SharedKernel.Events;


namespace ListaPreciosBC.Domain.Aggregates
{
    /// <summary>
    /// AGGREGATE ROOT que gobierna la configuración de columnas de la lista de precios.
    /// Invariante clave: toda modificación de la plantilla debe mantener sus propias reglas
    /// (1..10 columnas, una sola Base, orden/ids únicos y al menos una visible).
    /// </summary>
    [DebuggerDisplay("{Id} v{Version} - {Plantilla}")]
    public sealed class ListaPrecio
    {
        // -------- Identidad / Concurrencia / Auditoría --------
        public Guid Id { get; }
        public int Version { get; private set; }
        public DateTimeOffset? UltimaActualizacion { get; private set; }
        public string? UltimoUsuario { get; private set; }

        // -------- Estado gobernado por el agregado --------
        public PlantillaColumnasPrecio Plantilla { get; private set; }

    /// <summary>Id de la columna marcada como Base en la plantilla.</summary>
    public IdentificadorColumnaPrecio IdColumnaBase => Plantilla.IdColumnaBase;

    /// <summary>Número (1..10) de la columna Base.</summary>
    public byte NumeroColumnaBase => Plantilla.NumeroColumnaBase;

        // -------- Domain events (patrón simple) --------
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();

        private ListaPrecio(Guid id, PlantillaColumnasPrecio plantilla, int version = 0)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Plantilla = plantilla ?? throw new ArgumentNullException(nameof(plantilla));
            Version = version;
        }

        // =========================
        // Fábricas del agregado
        // =========================

        /// <summary>Crea una instancia con una plantilla ya validada.</summary>
        public static ListaPrecio CrearNueva(Guid id, PlantillaColumnasPrecio plantilla,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            var agg = new ListaPrecio(id, plantilla, 0);
            // opcional: emitir evento inicial como "actualizada" para sincronizar proyecciones
            agg.EmitirEventoActualizacion(usuario, cuando ?? DateTimeOffset.UtcNow);
            return agg;
        }

        /// <summary>
        /// Crea una configuración mínima por defecto:
        /// - P1: Base, visible, orden 1, modo Fijo, nombre "Precio de venta al público".
        /// </summary>
        public static ListaPrecio CrearConPlantillaPorDefecto(Guid id,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                id: IdentificadorColumnaPrecio.DesdeNumero(1),
                nombre: NombreColumnaPrecio.Crear("Precio de venta al público"),
                modo: ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg });
            return CrearNueva(id, plantilla, usuario, cuando);
        }

        // =========================
        // Comportamientos (wrappers sobre los VOs)
        // =========================

        public void RenombrarColumna(IdentificadorColumnaPrecio idColumna, NombreColumnaPrecio nuevoNombre,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = Plantilla.Renombrar(idColumna, nuevoNombre);
            EmitirEventoActualizacion(usuario, cuando);
        }

        public void CambiarModoColumna(IdentificadorColumnaPrecio idColumna, ModoValorizacionColumna nuevoModo,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = Plantilla.CambiarModo(idColumna, nuevoModo);
            EmitirEventoActualizacion(usuario, cuando);
        }

        public void MarcarColumnaComoBase(IdentificadorColumnaPrecio idColumna,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = Plantilla.MarcarComoBase(idColumna);
            EmitirEventoActualizacion(usuario, cuando);
        }

        public void MostrarColumna(IdentificadorColumnaPrecio idColumna,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = Plantilla.Mostrar(idColumna);
            EmitirEventoActualizacion(usuario, cuando);
        }

        public void OcultarColumna(IdentificadorColumnaPrecio idColumna,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = Plantilla.Ocultar(idColumna); // valida “no dejar sin visibles”
            EmitirEventoActualizacion(usuario, cuando);
        }

        public void CambiarOrdenColumna(IdentificadorColumnaPrecio idColumna, byte nuevoOrden,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = Plantilla.ConOrden(idColumna, nuevoOrden); // hace swap si está ocupado
            EmitirEventoActualizacion(usuario, cuando);
        }

        public void ReemplazarColumna(ConfiguracionColumnaPrecio nuevaCfg,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = Plantilla.Reemplazar(nuevaCfg);
            EmitirEventoActualizacion(usuario, cuando);
        }

        public void AgregarColumna(ConfiguracionColumnaPrecio nuevaCfg,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = Plantilla.Agregar(nuevaCfg);
            EmitirEventoActualizacion(usuario, cuando);
        }

        public void EliminarColumna(IdentificadorColumnaPrecio idColumna,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            var columna = Plantilla.Columnas.FirstOrDefault(c => c.Id == idColumna);
            if (columna == null)
                throw new ArgumentException("No existe la columna especificada.", nameof(idColumna));

            var puedeEliminarSpec = new ColumnaPuedeSerEliminadaSpecification();
            if (!puedeEliminarSpec.IsSatisfiedBy(columna))
                throw new InvalidOperationException("No se puede eliminar la columna base.");

            Plantilla = Plantilla.Eliminar(idColumna);
            EmitirEventoActualizacion(usuario, cuando);
        }

        /// <summary>
        /// Reemplaza completamente la plantilla (se revalidan todas las invariantes).
        /// </summary>
        public void EstablecerPlantilla(PlantillaColumnasPrecio nueva,
            string? usuario = null, DateTimeOffset? cuando = null)
        {
            Plantilla = nueva ?? throw new ArgumentNullException(nameof(nueva));
            EmitirEventoActualizacion(usuario, cuando);
        }

        // =========================
        // Internals
        // =========================

        private void EmitirEventoActualizacion(string? usuario, DateTimeOffset? cuando)
        {
            Version++;
            UltimaActualizacion = cuando ?? DateTimeOffset.UtcNow;
            UltimoUsuario = string.IsNullOrWhiteSpace(usuario) ? null : usuario;

            _domainEvents.Add(new PlantillaDeColumnasActualizada(
                ListaPrecioId: Id,
                NuevaPlantilla: Plantilla,
                Version: Version,
                Usuario: UltimoUsuario,
                OcurrioEn: UltimaActualizacion.Value));
        }
    }
}
