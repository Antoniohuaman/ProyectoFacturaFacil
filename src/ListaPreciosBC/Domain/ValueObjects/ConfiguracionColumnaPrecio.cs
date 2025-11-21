using System;
using System.Diagnostics;
using SharedKernel.Exceptions;

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
        /// - <see cref="Orden"/>: Posición visual (1..50). Único por plantilla.
    /// </summary>
        [DebuggerDisplay("{Orden}: {Id} - {Nombre} ({Modo}) Tipo={Tipo.Codigo} Base={EsBase} Visible={Visible}")]
    public sealed class ConfiguracionColumnaPrecio :
        IEquatable<ConfiguracionColumnaPrecio>, IComparable<ConfiguracionColumnaPrecio>
    {
        public const byte MinOrden = 1;
            public const byte MaxOrden = 50;

        public IdentificadorColumnaPrecio Id { get; }
        public NombreColumnaPrecio Nombre { get; }
        public ModoValorizacionColumna Modo { get; }
            public TipoColumnaPrecio Tipo { get; }
            public ReglaGlobalColumnaPrecio? ReglaGlobal { get; }
        public bool EsBase { get; }
        public bool Visible { get; }
        public byte Orden { get; }

        private ConfiguracionColumnaPrecio(
            IdentificadorColumnaPrecio id,
            NombreColumnaPrecio nombre,
            ModoValorizacionColumna modo,
            bool esBase,
            bool visible,
                byte orden,
                TipoColumnaPrecio tipo,
                ReglaGlobalColumnaPrecio? reglaGlobal)
        {
            Id     = id     ?? throw new ArgumentNullException(nameof(id));
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Modo   = modo   ?? throw new ArgumentNullException(nameof(modo));
                Tipo   = tipo   ?? throw new ArgumentNullException(nameof(tipo));

            if (orden < MinOrden || orden > MaxOrden)
                throw new ArgumentOutOfRangeException(nameof(orden), $"El orden debe estar entre {MinOrden} y {MaxOrden}.");

                ValidarConsistencia(esBase, tipo, reglaGlobal);

                ReglaGlobal = reglaGlobal;
                EsBase      = esBase;
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
            TipoColumnaPrecio? tipo = null,
            ReglaGlobalColumnaPrecio? reglaGlobal = null,
            bool esBase = false,
            bool visible = true,
            byte? orden = null)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (nombre == null) throw new ArgumentNullException(nameof(nombre));
            if (modo == null) throw new ArgumentNullException(nameof(modo));
            var o = orden ?? id.Numero; // por defecto igual al número de Pn
            var tipoResuelto = tipo ?? (esBase ? TipoColumnaPrecio.Base : TipoColumnaPrecio.Manual);
            var esBaseResuelto = tipoResuelto.EsBase;

            if (esBase && !tipoResuelto.EsBase)
                throw new BusinessRuleException("Solo las columnas de tipo Base pueden marcarse como base.");
            if (!esBase && tipoResuelto.EsBase)
                throw new BusinessRuleException("Las columnas de tipo Base siempre deben estar marcadas como base.");

            return new ConfiguracionColumnaPrecio(id, nombre, modo, esBaseResuelto, visible, o, tipoResuelto, reglaGlobal);
        }

        /// <summary>
        /// Try-crear sin lanzar excepciones.
        /// </summary>
        public static bool TryCrear(
            IdentificadorColumnaPrecio? id,
            NombreColumnaPrecio? nombre,
            ModoValorizacionColumna? modo,
            out ConfiguracionColumnaPrecio? cfg,
            TipoColumnaPrecio? tipo = null,
            ReglaGlobalColumnaPrecio? reglaGlobal = null,
            bool esBase = false,
            bool visible = true,
            byte? orden = null)
        {
            cfg = null;
            try
            {
                if (id is null || nombre is null || modo is null) return false;
                cfg = Crear(id, nombre, modo, tipo, reglaGlobal, esBase, visible, orden);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static ConfiguracionColumnaPrecio CrearBase(
            IdentificadorColumnaPrecio id,
            NombreColumnaPrecio nombre,
            ModoValorizacionColumna modo,
            bool visible = true,
            byte? orden = null)
            => Crear(id, nombre, modo, TipoColumnaPrecio.Base, null, esBase: true, visible: visible, orden: orden);

        public static ConfiguracionColumnaPrecio CrearGlobalDescuento(
            IdentificadorColumnaPrecio id,
            NombreColumnaPrecio nombre,
            ModoValorizacionColumna modo,
            ReglaGlobalColumnaPrecio regla,
            bool visible = true,
            byte? orden = null)
            => Crear(id, nombre, modo, TipoColumnaPrecio.GlobalDescuento, regla, esBase: false, visible: visible, orden: orden);

        public static ConfiguracionColumnaPrecio CrearGlobalRecargo(
            IdentificadorColumnaPrecio id,
            NombreColumnaPrecio nombre,
            ModoValorizacionColumna modo,
            ReglaGlobalColumnaPrecio regla,
            bool visible = true,
            byte? orden = null)
            => Crear(id, nombre, modo, TipoColumnaPrecio.GlobalRecargo, regla, esBase: false, visible: visible, orden: orden);

        public static ConfiguracionColumnaPrecio CrearMinimoPermitido(
            IdentificadorColumnaPrecio id,
            NombreColumnaPrecio nombre,
            ModoValorizacionColumna modo,
            ReglaGlobalColumnaPrecio? regla = null,
            bool visible = true,
            byte? orden = null)
            => Crear(id, nombre, modo, TipoColumnaPrecio.MinimoPermitido, regla, esBase: false, visible: visible, orden: orden);

        public static ConfiguracionColumnaPrecio CrearManual(
            IdentificadorColumnaPrecio id,
            NombreColumnaPrecio nombre,
            ModoValorizacionColumna modo,
            bool visible = true,
            byte? orden = null)
            => Crear(id, nombre, modo, TipoColumnaPrecio.Manual, null, esBase: false, visible: visible, orden: orden);

        // ----------------- Transformaciones inmutables (With-ers) -----------------

        public ConfiguracionColumnaPrecio Renombrar(NombreColumnaPrecio nuevoNombre)
            => new(Id, nuevoNombre ?? throw new ArgumentNullException(nameof(nuevoNombre)), Modo, EsBase, Visible, Orden, Tipo, ReglaGlobal);

        public ConfiguracionColumnaPrecio CambiarModo(ModoValorizacionColumna nuevoModo)
            => new(Id, Nombre, nuevoModo ?? throw new ArgumentNullException(nameof(nuevoModo)), EsBase, Visible, Orden, Tipo, ReglaGlobal);

        public ConfiguracionColumnaPrecio MarcarComoBase()
            => EsBase ? this : new(Id, Nombre, Modo, esBase: true, Visible, Orden, TipoColumnaPrecio.Base, null);

        public ConfiguracionColumnaPrecio DesmarcarComoBase()
            => !EsBase ? this : new(Id, Nombre, Modo, esBase: false, Visible, Orden, TipoColumnaPrecio.Manual, null);

        public ConfiguracionColumnaPrecio Mostrar()
            => Visible ? this : new(Id, Nombre, Modo, EsBase, visible: true, Orden, Tipo, ReglaGlobal);

        public ConfiguracionColumnaPrecio Ocultar()
            => !Visible ? this : new(Id, Nombre, Modo, EsBase, visible: false, Orden, Tipo, ReglaGlobal);

        public ConfiguracionColumnaPrecio ConOrden(byte nuevoOrden)
            => new(Id, Nombre, Modo, EsBase, Visible, nuevoOrden, Tipo, ReglaGlobal);

        // ----------------- Igualdad / Orden / ToString -----------------

        public bool Equals(ConfiguracionColumnaPrecio? other)
            => other is not null
               && Id.Equals(other.Id)
               && Nombre.Equals(other.Nombre)
               && Modo.Equals(other.Modo)
               && Tipo.Equals(other.Tipo)
               && Equals(ReglaGlobal, other.ReglaGlobal)
               && EsBase == other.EsBase
               && Visible == other.Visible
               && Orden == other.Orden;

        public override bool Equals(object? obj) => Equals(obj as ConfiguracionColumnaPrecio);

        public override int GetHashCode()
            => HashCode.Combine(Id, Nombre, Modo, Tipo, ReglaGlobal, EsBase, Visible, Orden);

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
            => $"{Orden}: {Id}/{Nombre} [{Modo}] Tipo={Tipo.Codigo} {(EsBase ? "Base" : "")} {(Visible ? "Visible" : "Oculta")}".Trim();

        private static void ValidarConsistencia(bool esBase, TipoColumnaPrecio tipo, ReglaGlobalColumnaPrecio? regla)
        {
            if (tipo.EsBase && !esBase)
            {
                throw new BusinessRuleException("Las columnas de tipo Base deben estar marcadas como base.");
            }

            if (!tipo.EsBase && esBase)
            {
                throw new BusinessRuleException("Solo las columnas de tipo Base pueden estar marcadas como base.");
            }

            if (tipo.EsGlobal && regla is null)
            {
                throw new BusinessRuleException("Las columnas globales requieren una regla global configurada.");
            }

            if (!tipo.EsGlobal && !tipo.EsMinimoPermitido && regla is not null)
            {
                throw new BusinessRuleException("Solo las columnas globales o de mínimo permitido pueden tener reglas globales.");
            }
        }
    }
}