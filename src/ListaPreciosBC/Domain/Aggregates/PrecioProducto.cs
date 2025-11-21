using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ListaPreciosBC.Domain.Entities;
using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.Specifications;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // ProductoId, EmpresaId, UnidadDeMedida

namespace ListaPreciosBC.Domain.Aggregates
{
    /// <summary>
    /// AGGREGATE ROOT que gobierna los precios de un Producto por columna (P1..P10).
    /// En cada columna puede existir:
    ///   - Precio Fijo + Periodo de Vigencia, o
    ///   - Matriz por Volumen (tramos de cantidad → precio).
    /// Exclusividad por columna: sólo uno de los dos.
    /// </summary>
    [DebuggerDisplay("{ProductoId} v{Version} (Entradas={_preciosPorUnidad.Count})")]
    public sealed class PrecioProducto
    {
        // ------------ Identidad / Concurrencia / Auditoría ------------
        /// <summary>Tenant al que pertenece este agregado.</summary>
        public EmpresaId EmpresaId { get; }

    /// <summary>Establecimiento/Sucursal al que pertenece este agregado (opcional).</summary>
    public Guid? EstablecimientoId { get; }

        /// <summary>Identidad opaca del producto.</summary>
        public ProductoId ProductoId { get; }
        public int Version { get; private set; }
        public DateTimeOffset? UltimaActualizacion { get; private set; }
        public string? UltimoUsuario { get; private set; }

        private static UnidadDeMedida UnidadPorDefecto => UnidadDeMedida.NIU;

        // ------------ Estado ------------
        // Clave por par (Id de columna, Unidad de medida)
        private readonly Dictionary<(byte Columna, string Unidad), PrecioPorUnidadDeMedida> _preciosPorUnidad = new();

        public IReadOnlyCollection<PrecioPorUnidadDeMedida> PreciosPorUnidad => _preciosPorUnidad.Values;

        // ------------ Domain events (acumulados hasta publicar) ------------
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();

        private PrecioProducto(EmpresaId empresaId, ProductoId productoId, Guid? establecimientoId)
        {
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            ProductoId = productoId;
            EstablecimientoId = establecimientoId;
        }

        private static string NormalizarUnidad(UnidadDeMedida unidad)
            => (unidad ?? throw new ArgumentNullException(nameof(unidad))).Codigo.ToUpperInvariant();

        private static (byte Columna, string Unidad) Clave(IdentificadorColumnaPrecio columna, UnidadDeMedida unidad)
            => ((columna ?? throw new ArgumentNullException(nameof(columna))).Numero, NormalizarUnidad(unidad));

        private PrecioPorUnidadDeMedida ObtenerOCrearRegistro(IdentificadorColumnaPrecio columna, UnidadDeMedida unidad)
        {
            var key = Clave(columna, unidad);
            if (!_preciosPorUnidad.TryGetValue(key, out var registro))
            {
                registro = new PrecioPorUnidadDeMedida(columna, unidad);
                _preciosPorUnidad[key] = registro;
            }

            return registro;
        }

        private void RemoverRegistroSiVacio((byte Columna, string Unidad) key, PrecioPorUnidadDeMedida registro)
        {
            if (registro.EstaVacia)
            {
                _preciosPorUnidad.Remove(key);
            }
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
        public static PrecioProducto CrearNuevo(EmpresaId empresaId, ProductoId productoId, Guid? establecimientoId = null)
            => new(empresaId, productoId, establecimientoId);

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
            UnidadDeMedida unidadDeMedida,
            ValorPrecio valor,
            PeriodoVigencia vigencia,
            string? usuario = null,
            DateTimeOffset? cuando = null,
            int cantidadReferenciaParaEventoBase = 1)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));
            if (unidadDeMedida is null) throw new ArgumentNullException(nameof(unidadDeMedida));
            if (valor   is null) throw new ArgumentNullException(nameof(valor));
            if (vigencia is null) throw new ArgumentNullException(nameof(vigencia));

            // Validación de periodo de vigencia usando la policy
            if (!PuedeEstablecerPeriodoVigenciaPolicy.Validar(vigencia.Desde, vigencia.Hasta))
                throw new InvalidOperationException("El periodo de vigencia no es válido.");

            var registro = ObtenerOCrearRegistro(columna, unidadDeMedida);
            registro.EstablecerPrecioFijo(valor, vigencia);

            Versionar(usuario, cuando);
            _domainEvents.Add(new PrecioColumnaActualizada(EmpresaId, EstablecimientoId, ProductoId, columna, UltimaActualizacion!.Value));
            _domainEvents.Add(new PrecioFijoActualizado(EmpresaId, EstablecimientoId, ProductoId, columna, UltimaActualizacion!.Value));

            // Si es Base (P1) y está vigente a "cuando", publicar evento específico
            if (columna.Numero == 1 && vigencia.Contiene(UltimaActualizacion!.Value))
            {
                _domainEvents.Add(new PrecioBaseVigenteEstablecido(
                    EmpresaId,
                    EstablecimientoId,
                    ProductoId,
                    columna,
                    new PrecioResuelto(valor, PrecioResueltoOrigen.Fijo, Math.Max(1, cantidadReferenciaParaEventoBase)),
                    UltimaActualizacion!.Value));
            }
        }

        public void UpsertPrecioFijo(
            IdentificadorColumnaPrecio columna,
            ValorPrecio valor,
            PeriodoVigencia vigencia,
            string? usuario = null,
            DateTimeOffset? cuando = null,
            int cantidadReferenciaParaEventoBase = 1)
            => UpsertPrecioFijo(columna, UnidadPorDefecto, valor, vigencia, usuario, cuando, cantidadReferenciaParaEventoBase);

        /// <summary>Elimina el precio fijo de la columna/unidad (si existe).</summary>
        public void EliminarPrecioFijo(IdentificadorColumnaPrecio columna, UnidadDeMedida unidadDeMedida, string? usuario = null, DateTimeOffset? cuando = null)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));
            if (unidadDeMedida is null) throw new ArgumentNullException(nameof(unidadDeMedida));

            var key = Clave(columna, unidadDeMedida);
            if (_preciosPorUnidad.TryGetValue(key, out var registro) && registro.TienePrecioFijo)
            {
                registro.EliminarPrecioFijo();
                RemoverRegistroSiVacio(key, registro);

                Versionar(usuario, cuando);
                _domainEvents.Add(new PrecioColumnaActualizada(EmpresaId, EstablecimientoId, ProductoId, columna, UltimaActualizacion!.Value));
            }
        }

        public void EliminarPrecioFijo(IdentificadorColumnaPrecio columna, string? usuario = null, DateTimeOffset? cuando = null)
            => EliminarPrecioFijo(columna, UnidadPorDefecto, usuario, cuando);

        /// <summary>
        /// Crea/actualiza una matriz por volumen para la columna.
        /// Si existía precio fijo, se elimina (exclusividad).
        /// </summary>
        public void UpsertMatrizVolumen(
            IdentificadorColumnaPrecio columna,
            UnidadDeMedida unidadDeMedida,
            MatrizVolumen matriz,
            string? usuario = null,
            DateTimeOffset? cuando = null,
            int cantidadReferenciaParaEventoBase = 1)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));
            if (unidadDeMedida is null) throw new ArgumentNullException(nameof(unidadDeMedida));
            if (matriz   is null) throw new ArgumentNullException(nameof(matriz));

            var registro = ObtenerOCrearRegistro(columna, unidadDeMedida);
            registro.EstablecerMatrizVolumen(matriz);

            Versionar(usuario, cuando);
            _domainEvents.Add(new MatrizVolumenActualizada(EmpresaId, EstablecimientoId, ProductoId, columna, UltimaActualizacion!.Value));

            // Si es Base (P1), publicar evento con el tramo para la cantidad de referencia (si existe)
            if (columna.Numero == 1)
            {
                var cant = Math.Max(1, cantidadReferenciaParaEventoBase);
                var tramo = matriz.ObtenerTramo(cant);
                if (tramo is not null)
                {
                    _domainEvents.Add(new PrecioBaseVigenteEstablecido(
                        EmpresaId,
                        EstablecimientoId,
                        ProductoId,
                        columna,
                        new PrecioResuelto(tramo.Precio, PrecioResueltoOrigen.PorVolumen, cant),
                        UltimaActualizacion!.Value));
                }
            }
        }

        public void UpsertMatrizVolumen(
            IdentificadorColumnaPrecio columna,
            MatrizVolumen matriz,
            string? usuario = null,
            DateTimeOffset? cuando = null,
            int cantidadReferenciaParaEventoBase = 1)
            => UpsertMatrizVolumen(columna, UnidadPorDefecto, matriz, usuario, cuando, cantidadReferenciaParaEventoBase);

        /// <summary>Elimina la matriz por volumen de la columna/unidad (si existe).</summary>
        public void EliminarMatrizVolumen(IdentificadorColumnaPrecio columna, UnidadDeMedida unidadDeMedida, string? usuario = null, DateTimeOffset? cuando = null)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));
            if (unidadDeMedida is null) throw new ArgumentNullException(nameof(unidadDeMedida));

            var key = Clave(columna, unidadDeMedida);
            if (_preciosPorUnidad.TryGetValue(key, out var registro) && registro.TieneMatrizVolumen)
            {
                registro.EliminarMatrizVolumen();
                RemoverRegistroSiVacio(key, registro);

                Versionar(usuario, cuando);
                _domainEvents.Add(new MatrizVolumenActualizada(EmpresaId, EstablecimientoId, ProductoId, columna, UltimaActualizacion!.Value));
            }
        }

        public void EliminarMatrizVolumen(IdentificadorColumnaPrecio columna, string? usuario = null, DateTimeOffset? cuando = null)
            => EliminarMatrizVolumen(columna, UnidadPorDefecto, usuario, cuando);

        /// <summary>
        /// Resuelve el precio vigente para la columna, fecha y cantidad indicadas.
        /// 1) Si hay Fijo vigente → ese.
        /// 2) De lo contrario, si hay Matriz, busca tramo por cantidad.
        /// 3) Si nada aplica → null.
        /// </summary>
        public PrecioResuelto? ObtenerPrecioVigente(
            IdentificadorColumnaPrecio columna,
            UnidadDeMedida unidadDeMedida,
            DateTimeOffset fecha,
            int cantidad)
        {
            if (columna is null) throw new ArgumentNullException(nameof(columna));
            if (unidadDeMedida is null) throw new ArgumentNullException(nameof(unidadDeMedida));
            if (cantidad < 1) return null;

            if (!_preciosPorUnidad.TryGetValue(Clave(columna, unidadDeMedida), out var registro))
                return null;

            if (registro.TienePrecioFijo && registro.Vigencia!.Contiene(fecha))
                return new PrecioResuelto(registro.PrecioFijo!, PrecioResueltoOrigen.Fijo, cantidad);

            if (registro.MatrizVolumen is not null)
            {
                var tramo = registro.MatrizVolumen.ObtenerTramo(cantidad);
                if (tramo is not null)
                    return new PrecioResuelto(tramo.Precio, PrecioResueltoOrigen.PorVolumen, cantidad);
            }

            return null;
        }

        public PrecioResuelto? ObtenerPrecioVigente(
            IdentificadorColumnaPrecio columna,
            DateTimeOffset fecha,
            int cantidad)
            => ObtenerPrecioVigente(columna, UnidadPorDefecto, fecha, cantidad);

        // =========================
        // Internals
        // =========================

        private void Versionar(string? usuario, DateTimeOffset? cuando)
        {
            Version++;
            UltimaActualizacion = cuando ?? DateTimeOffset.UtcNow;
            UltimoUsuario = string.IsNullOrWhiteSpace(usuario) ? null : usuario;
        }

    }
}
