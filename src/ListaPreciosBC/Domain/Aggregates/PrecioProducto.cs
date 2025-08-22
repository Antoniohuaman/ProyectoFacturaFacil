using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.Specifications;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // Sku

namespace ListaPreciosBC.Domain.Aggregates
{
    /// <summary>
    /// AGGREGATE ROOT que gobierna los precios de un SKU por columna (P1..P10).
    /// En cada columna puede existir:
    ///   - Precio Fijo + Periodo de Vigencia, o
    ///   - Matriz por Volumen (tramos de cantidad → precio).
    /// Exclusividad por columna: sólo uno de los dos.
    /// </summary>
    [DebuggerDisplay("{Sku} v{Version} (Fijos={_preciosFijos.Count}, Volumen={_matricesVolumen.Count})")]
    public sealed class PrecioProducto
    {
        // ------------ Identidad / Concurrencia / Auditoría ------------
        public Sku Sku { get; }
        public int Version { get; private set; }
        public DateTimeOffset? UltimaActualizacion { get; private set; }
        public string? UltimoUsuario { get; private set; }

        // ------------ Estado ------------
        // Clave por Id de columna (P1..P10 => 1..10)
        private readonly Dictionary<byte, PrecioFijo> _preciosFijos = new();
        private readonly Dictionary<byte, MatrizVolumen> _matricesVolumen = new();

        public IReadOnlyDictionary<byte, PrecioFijo> PreciosFijos => _preciosFijos;
        public IReadOnlyDictionary<byte, MatrizVolumen> MatricesVolumen => _matricesVolumen;

        // ------------ Domain events (acumulados hasta publicar) ------------
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();

        private PrecioProducto(Sku sku)
        {
            Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        }

        // Ejemplo de uso de la policy en un método relevante
        private void ValidarPeriodoVigencia(DateTime desde, DateTime? hasta)
        {
            if (!PuedeEstablecerPeriodoVigenciaPolicy.Validar(desde, hasta))
                throw new InvalidOperationException("El periodo de vigencia no es válido.");
        }

        // Ejemplo de uso de la policy PuedeCambiarModoColumnaPolicy
        private void ValidarModoColumna(string nuevoModo)
        {
            if (!PuedeCambiarModoColumnaPolicy.Validar(nuevoModo))
                throw new InvalidOperationException("El modo de valorización no es válido.");
        }

        // Ejemplo de uso de la policy PuedeEliminarColumnaPolicy
        private void ValidarEliminacionColumna(IdentificadorColumnaPrecio columnaAEliminar, IEnumerable<ConfiguracionColumnaPrecio> columnas)
        {
            if (!PuedeEliminarColumnaPolicy.Validar(columnaAEliminar, columnas))
                throw new InvalidOperationException("No se puede eliminar la columna: debe quedar al menos una visible y una base.");
        }
            /// <summary>
            /// Determina si una columna cumple con la especificación ColumnaPuedeSerBaseSpecification.
            /// Una columna puede ser base si es visible y no es ya base.
            /// </summary>
            private bool EsColumnaQuePuedeSerBase(ConfiguracionColumnaPrecio columna)
            {
                var especificacion = new ColumnaPuedeSerBaseSpecification();
                return especificacion.IsSatisfiedBy(columna);
            }
                /// <summary>
                /// Determina si una columna cumple con la especificación ColumnaPuedeSerEliminadaSpecification.
                /// Una columna puede ser eliminada si no es base.
                /// </summary>
                private bool EsColumnaQuePuedeSerEliminada(ConfiguracionColumnaPrecio columna)
                {
                    var especificacion = new ColumnaPuedeSerEliminadaSpecification();
                    return especificacion.IsSatisfiedBy(columna);
                }
                    /// <summary>
                    /// Determina si una colección de columnas cumple con la especificación PlantillaTieneColumnaBaseSpecification.
                    /// Retorna true si al menos una columna es base.
                    /// </summary>
                    private bool PlantillaTieneColumnaBase(IEnumerable<ConfiguracionColumnaPrecio> columnas)
                    {
                        var especificacion = new PlantillaTieneColumnaBaseSpecification();
                        return especificacion.IsSatisfiedBy(columnas);
                    }
                        /// <summary>
                        /// Determina si una colección de columnas cumple con la especificación PlantillaColumnasUnicasSpecification.
                        /// Retorna true si todas las columnas son únicas por identificador.
                        /// </summary>
                        private bool PlantillaColumnasSonUnicas(IEnumerable<ConfiguracionColumnaPrecio> columnas)
                        {
                            var especificacion = new PlantillaColumnasUnicasSpecification();
                            return especificacion.IsSatisfiedBy(columnas);
                        }
            /// <summary>
            /// Determina si una columna de precio es la columna base usando la especificación ColumnaBaseSpecification.
            /// </summary>
            private bool EsColumnaBase(ConfiguracionColumnaPrecio columna)
            {
                var spec = new ColumnaBaseSpecification();
                return spec.IsSatisfiedBy(columna);
            }

            /// <summary>
            /// Determina si una columna de precio tiene modo fijo usando la especificación ColumnaModoFijoSpecification.
            /// </summary>
            private bool EsColumnaModoFijo(ConfiguracionColumnaPrecio columna)
            {
                var spec = new ColumnaModoFijoSpecification();
                return spec.IsSatisfiedBy(columna);
            }
        // =========================
        // Fábrica
        // =========================
        public static PrecioProducto CrearNuevo(Sku sku) => new(sku);

        // =========================
        // Comportamientos
        // =========================

        /// <summary>
        /// Crea/actualiza un precio fijo para la columna.
        /// Si existía matriz por volumen en esa columna, se elimina (exclusividad).
        /// </summary>
        /// <param name="cantidadReferenciaParaEventoBase">
        /// Si la columna es P1 se emite <see cref="PrecioBaseVigenteEstablecido"/> usando esta cantidad (por defecto 1).
        /// </param>
        public void UpsertPrecioFijo(
            IdentificadorColumnaPrecio columna,
            ValorPrecio valor,
            PeriodoVigencia vigencia,
            string? usuario = null,
            DateTimeOffset? cuando = null,
            int cantidadReferenciaParaEventoBase = 1)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));
            if (valor   is null) throw new ArgumentNullException(nameof(valor));
            if (vigencia is null) throw new ArgumentNullException(nameof(vigencia));

            // Validación de periodo de vigencia usando la policy
            if (!PuedeEstablecerPeriodoVigenciaPolicy.Validar(vigencia.Desde, vigencia.Hasta))
                throw new InvalidOperationException("El periodo de vigencia no es válido.");

            var key = columna.Numero;

            _matricesVolumen.Remove(key);                 // exclusividad
            _preciosFijos[key] = new PrecioFijo(valor, vigencia);

            Versionar(usuario, cuando);
            _domainEvents.Add(new PrecioColumnaActualizada(Sku, columna, UltimaActualizacion!.Value));

            // Si es Base (P1) y está vigente a "cuando", publicar evento específico
            if (key == 1 && vigencia.Contiene(UltimaActualizacion!.Value))
            {
                _domainEvents.Add(new PrecioBaseVigenteEstablecido(
                    Sku,
                    columna,
                    new PrecioResuelto(valor, PrecioResueltoOrigen.Fijo, Math.Max(1, cantidadReferenciaParaEventoBase)),
                    UltimaActualizacion!.Value));
            }
        }

        /// <summary>Elimina el precio fijo de la columna (si existe).</summary>
        public void EliminarPrecioFijo(IdentificadorColumnaPrecio columna, string? usuario = null, DateTimeOffset? cuando = null)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));

            var key = columna.Numero;
            if (_preciosFijos.Remove(key))
            {
                Versionar(usuario, cuando);
                _domainEvents.Add(new PrecioColumnaActualizada(Sku, columna, UltimaActualizacion!.Value));
            }
        }

        /// <summary>
        /// Crea/actualiza una matriz por volumen para la columna.
        /// Si existía precio fijo, se elimina (exclusividad).
        /// </summary>
        public void UpsertMatrizVolumen(
            IdentificadorColumnaPrecio columna,
            MatrizVolumen matriz,
            string? usuario = null,
            DateTimeOffset? cuando = null,
            int cantidadReferenciaParaEventoBase = 1)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));
            if (matriz   is null) throw new ArgumentNullException(nameof(matriz));

            var key = columna.Numero;

            _preciosFijos.Remove(key);           // exclusividad
            _matricesVolumen[key] = matriz;

            Versionar(usuario, cuando);
            _domainEvents.Add(new MatrizVolumenActualizada(Sku, columna, UltimaActualizacion!.Value));

            // Si es Base (P1), publicar evento con el tramo para la cantidad de referencia (si existe)
            if (key == 1)
            {
                var cant = Math.Max(1, cantidadReferenciaParaEventoBase);
                var tramo = matriz.ObtenerTramo(cant);
                if (tramo is not null)
                {
                    _domainEvents.Add(new PrecioBaseVigenteEstablecido(
                        Sku,
                        columna,
                        new PrecioResuelto(tramo.Precio, PrecioResueltoOrigen.PorVolumen, cant),
                        UltimaActualizacion!.Value));
                }
            }
        }

        /// <summary>Elimina la matriz por volumen de la columna (si existe).</summary>
        public void EliminarMatrizVolumen(IdentificadorColumnaPrecio columna, string? usuario = null, DateTimeOffset? cuando = null)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));

            var key = columna.Numero;
            if (_matricesVolumen.Remove(key))
            {
                Versionar(usuario, cuando);
                _domainEvents.Add(new MatrizVolumenActualizada(Sku, columna, UltimaActualizacion!.Value));
            }
        }

        /// <summary>
        /// Resuelve el precio vigente para la columna, fecha y cantidad indicadas.
        /// 1) Si hay Fijo vigente → ese.
        /// 2) De lo contrario, si hay Matriz, busca tramo por cantidad.
        /// 3) Si nada aplica → null.
        /// </summary>
        public PrecioResuelto? ObtenerPrecioVigente(
            IdentificadorColumnaPrecio columna,
            DateTimeOffset fecha,
            int cantidad)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));
            if (cantidad < 1) return null;

            var key = columna.Numero;

            if (_preciosFijos.TryGetValue(key, out var fijo) && fijo.Vigencia.Contiene(fecha))
                return new PrecioResuelto(fijo.Valor, PrecioResueltoOrigen.Fijo, cantidad);

            if (_matricesVolumen.TryGetValue(key, out var matriz))
            {
                var tramo = matriz.ObtenerTramo(cantidad);
                if (tramo is not null)
                    return new PrecioResuelto(tramo.Precio, PrecioResueltoOrigen.PorVolumen, cantidad);
            }

            return null;
        }

        // =========================
        // Internals
        // =========================

        private void Versionar(string? usuario, DateTimeOffset? cuando)
        {
            Version++;
            UltimaActualizacion = cuando ?? DateTimeOffset.UtcNow;
            UltimoUsuario = string.IsNullOrWhiteSpace(usuario) ? null : usuario;
        }

        // ------------- Estructura interna para fijo -------------
        public sealed record PrecioFijo(ValorPrecio Valor, PeriodoVigencia Vigencia);
    }
}
