using System;
using System.Diagnostics;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Configuración de una columna de la lista de precios.
    /// Inmutable. Igualdad por valor. Orden natural por <see cref="Orden"/>, luego por <see cref="Id"/>.
    ///
    /// Componentes:
    /// - <see cref="Id"/>: Identificador estable (P1..P10).
    /// - <see cref="Nombre"/>: Nombre visible y editable (VO con normalización/validación).
    /// - <see cref="Modo"/>: Fijo | PorVolumen (smart-enum/VO).
    /// - <see cref="EsBase"/>: Marca si esta columna es la "base" (unicidad garantizada por la plantilla).
    /// - <see cref="Visible"/>: Si se muestra en UI (la plantilla asegura que haya al menos una visible).
    /// - <see cref="Orden"/>: Posición visual (1..10). Único por plantilla.
    /// </summary>
    [DebuggerDisplay("{Orden}: {Id} - {Nombre} ({Modo}) Base={EsBase} Visible={Visible}")]
    public sealed class ConfiguracionColumnaPrecio :
        IEquatable<ConfiguracionColumnaPrecio>, IComparable<ConfiguracionColumnaPrecio>
    {
        public const byte MinOrden = 1;
        public const byte MaxOrden = 10;

        public IdentificadorColumnaPrecio Id { get; }
        public NombreColumnaPrecio Nombre { get; }
        public ModoValorizacionColumna Modo { get; }
        public bool EsBase { get; }
        public bool Visible { get; }
        public byte Orden { get; }

        private ConfiguracionColumnaPrecio(
            IdentificadorColumnaPrecio id,
            NombreColumnaPrecio nombre,
            ModoValorizacionColumna modo,
            bool esBase,
            bool visible,
            byte orden)
        {
            Id     = id     ?? throw new ArgumentNullException(nameof(id));
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Modo   = modo   ?? throw new ArgumentNullException(nameof(modo));

            if (orden < MinOrden || orden > MaxOrden)
                throw new ArgumentOutOfRangeException(nameof(orden), $"El orden debe estar entre {MinOrden} y {MaxOrden}.");

            EsBase  = esBase;
            Visible = visible;
            Orden   = orden;
        }

        /// <summary>
        /// Crea una configuración. Si <paramref name="orden"/> es null, toma <c>id.Numero</c>.
        /// </summary>
        public static ConfiguracionColumnaPrecio Crear(
            IdentificadorColumnaPrecio id,
            NombreColumnaPrecio nombre,
            ModoValorizacionColumna modo,
            bool esBase = false,
            bool visible = true,
            byte? orden = null)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (nombre == null) throw new ArgumentNullException(nameof(nombre));
            if (modo == null) throw new ArgumentNullException(nameof(modo));
            var o = orden ?? id.Numero; // por defecto igual al número de Pn
            return new ConfiguracionColumnaPrecio(id, nombre, modo, esBase, visible, o);
        }

        /// <summary>
        /// Try-crear sin lanzar excepciones.
        /// </summary>
        public static bool TryCrear(
            IdentificadorColumnaPrecio? id,
            NombreColumnaPrecio? nombre,
            ModoValorizacionColumna? modo,
            out ConfiguracionColumnaPrecio? cfg,
            bool esBase = false,
            bool visible = true,
            byte? orden = null)
        {
            cfg = null;
            try
            {
                if (id is null || nombre is null || modo is null) return false;
                cfg = Crear(id, nombre, modo, esBase, visible, orden);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ----------------- Transformaciones inmutables (With-ers) -----------------

        public ConfiguracionColumnaPrecio Renombrar(NombreColumnaPrecio nuevoNombre)
            => new(Id, nuevoNombre ?? throw new ArgumentNullException(nameof(nuevoNombre)), Modo, EsBase, Visible, Orden);

        public ConfiguracionColumnaPrecio CambiarModo(ModoValorizacionColumna nuevoModo)
            => new(Id, Nombre, nuevoModo ?? throw new ArgumentNullException(nameof(nuevoModo)), EsBase, Visible, Orden);

        public ConfiguracionColumnaPrecio MarcarComoBase()
            => EsBase ? this : new(Id, Nombre, Modo, esBase: true,  Visible, Orden);

        public ConfiguracionColumnaPrecio DesmarcarComoBase()
            => !EsBase ? this : new(Id, Nombre, Modo, esBase: false, Visible, Orden);

        public ConfiguracionColumnaPrecio Mostrar()
            => Visible ? this : new(Id, Nombre, Modo, EsBase, visible: true,  Orden);

        public ConfiguracionColumnaPrecio Ocultar()
            => !Visible ? this : new(Id, Nombre, Modo, EsBase, visible: false, Orden);

        public ConfiguracionColumnaPrecio ConOrden(byte nuevoOrden)
            => new(Id, Nombre, Modo, EsBase, Visible, nuevoOrden);

        // ----------------- Igualdad / Orden / ToString -----------------

        public bool Equals(ConfiguracionColumnaPrecio? other)
            => other is not null
               && Id.Equals(other.Id)
               && Nombre.Equals(other.Nombre)
               && Modo.Equals(other.Modo)
               && EsBase == other.EsBase
               && Visible == other.Visible
               && Orden == other.Orden;

        public override bool Equals(object? obj) => Equals(obj as ConfiguracionColumnaPrecio);

        public override int GetHashCode()
            => HashCode.Combine(Id, Nombre, Modo, EsBase, Visible, Orden);

        /// <summary>
        /// Orden natural: por Orden ascendente, luego por Id.Numero.
        /// </summary>
        public int CompareTo(ConfiguracionColumnaPrecio? other)
        {
            if (other is null) return 1;
            var cmp = Orden.CompareTo(other.Orden);
            if (cmp != 0) return cmp;
            return Id.Numero.CompareTo(other.Id.Numero);
        }

        public override string ToString()
            => $"{Orden}: {Id}/{Nombre} [{Modo}] {(EsBase ? "Base" : "")} {(Visible ? "Visible" : "Oculta")}".Trim();
    }
}