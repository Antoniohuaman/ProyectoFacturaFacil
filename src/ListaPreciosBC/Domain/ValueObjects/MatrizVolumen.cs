using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Conjunto inmutable de <see cref="TramoVolumen"/>:
    /// - Siempre <b>ordenado</b> por rango (Min asc, luego Max; null=∞ al final).
    /// - <b>Sin solapes</b> (intersección vacía entre tramos).
    /// - Moneda e indicador <c>IncluyeImpuesto</c> <b>consistentes</b> en todos los tramos.
    /// - Opcionalmente <b>continuo</b> desde 1 (sin huecos) si se solicita.
    ///
    /// Ofrece búsqueda de tramo por cantidad, inserción/reemplazo/eliminación (retornan nueva instancia)
    /// y <b>colapso automático</b> de tramos contiguos con el mismo precio (opcional, por defecto activado).
    /// </summary>
    [DebuggerDisplay("Tramos={Count}")]
    public sealed class MatrizVolumen : IEquatable<MatrizVolumen>
    {
        private readonly IReadOnlyList<TramoVolumen> _tramos;

        /// <summary>Devuelve los tramos (copia inmutable ordenada).</summary>
        public IReadOnlyList<TramoVolumen> Tramos => _tramos;

        /// <summary>Cantidad de tramos.</summary>
        public int Count => _tramos.Count;

        private MatrizVolumen(IReadOnlyList<TramoVolumen> tramosOrdenados)
        {
            _tramos = tramosOrdenados;
        }

        // -------------------------------------------------------------------------------------
        // Fábricas
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Crea una matriz a partir de una secuencia de tramos.
        /// Valida: sin solapes, consistencia de moneda &amp; flag de impuestos.
        /// Opcionalmente: continuidad desde 1 y colapso de contiguos con mismo precio.
        /// </summary>
        /// <param name="tramos">Tramos de entrada (pueden venir desordenados).</param>
        /// <param name="exigirContinuidadDesdeUno">Si true, exige cobertura continua desde cantidad 1.</param>
        /// <param name="colapsarContiguosIgualPrecio">Si true, une tramos contiguos con el mismo precio.</param>
        public static MatrizVolumen Crear(
            IEnumerable<TramoVolumen> tramos,
            bool exigirContinuidadDesdeUno = false,
            bool colapsarContiguosIgualPrecio = true)
        {
            if (tramos is null) throw new ArgumentNullException(nameof(tramos));

            var ordenados = tramos.OrderBy(t => t).ToList();

            ValidarSinSolapes(ordenados);
            ValidarConsistenciaPrecio(ordenados);

            if (exigirContinuidadDesdeUno)
                ValidarContinuidadDesdeUno(ordenados);

            if (colapsarContiguosIgualPrecio && ordenados.Count > 1)
                ordenados = ColapsarIgualesContiguos(ordenados);

            return new MatrizVolumen(ordenados);
        }

        /// <summary>
        /// Crea una matriz vacía (sin tramos).
        /// </summary>
        public static MatrizVolumen Vacia() => new(Array.Empty<TramoVolumen>());

        // -------------------------------------------------------------------------------------
        // Consultas
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Devuelve el tramo aplicable a la <paramref name="cantidad"/> (o null si ningún tramo coincide).
        /// </summary>
        public TramoVolumen? ObtenerTramo(int cantidad)
        {
            if (cantidad < 1 || _tramos.Count == 0) return null;

            // Lista pequeña: búsqueda lineal suficiente y clara.
            foreach (var t in _tramos)
                if (t.ContieneCantidad(cantidad))
                    return t;

            return null;
        }

        /// <summary>
        /// Moneda de todos los tramos (lanza si no hay tramos o si hay inconsistencia).
        /// </summary>
        public SharedKernel.ValueObjects.Moneda Moneda
        {
            get
            {
                if (_tramos.Count == 0)
                    throw new InvalidOperationException("La matriz no tiene tramos.");

                var m = _tramos[0].Precio.Importe.Moneda;
                if (_tramos.Any(t => t.Precio.Importe.Moneda != m))
                    throw new InvalidOperationException("Moneda inconsistente entre tramos.");

                return m;
            }
        }

        /// <summary>
        /// Verdadero si todos los tramos comparten el mismo valor de IncluyeImpuesto.
        /// Lanza si la matriz está vacía.
        /// </summary>
        public bool IncluyeImpuesto
        {
            get
            {
                if (_tramos.Count == 0)
                    throw new InvalidOperationException("La matriz no tiene tramos.");

                var v = _tramos[0].Precio.IncluyeImpuesto;
                if (_tramos.Any(t => t.Precio.IncluyeImpuesto != v))
                    throw new InvalidOperationException("Flag IncluyeImpuesto inconsistente entre tramos.");
                return v;
            }
        }

        // -------------------------------------------------------------------------------------
        // Transformaciones (inmutables)
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Inserta un tramo. Lanza si solapa con alguno existente.
        /// Si es contiguo y con mismo precio que el vecino, se colapsa automáticamente.
        /// </summary>
        public MatrizVolumen Insertar(TramoVolumen nuevo, bool colapsarContiguosIgualPrecio = true)
        {
            if (nuevo is null) throw new ArgumentNullException(nameof(nuevo));

            // Validar consistencia de precio respecto de los tramos existentes
            if (_tramos.Count > 0)
            {
                var mon = _tramos[0].Precio.Importe.Moneda;
                var inc = _tramos[0].Precio.IncluyeImpuesto;
                if (nuevo.Precio.Importe.Moneda != mon)
                    throw new InvalidOperationException("Moneda del nuevo tramo difiere de la matriz.");
                if (nuevo.Precio.IncluyeImpuesto != inc)
                    throw new InvalidOperationException("IncluyeImpuesto del nuevo tramo difiere de la matriz.");
            }

            var lista = _tramos.ToList();
            // Insertar ordenado
            var idx = lista.BinarySearch(nuevo, Comparer<TramoVolumen>.Create((a, b) => a.CompareTo(b)));
            if (idx < 0) idx = ~idx;
            lista.Insert(idx, nuevo);

            // Validar solapes tras insertar
            ValidarSinSolapes(lista);

            // Colapsar si procede
            if (colapsarContiguosIgualPrecio && lista.Count > 1)
                lista = ColapsarIgualesContiguos(lista);

            return new MatrizVolumen(lista);
        }

        /// <summary>
        /// Reemplaza un tramo existente (por igualdad de rango: Min/Max) por otro.
        /// Lanza si el tramo a reemplazar no existe o si el nuevo provoca solapes.
        /// </summary>
        public MatrizVolumen Reemplazar(TramoVolumen existente, TramoVolumen nuevo, bool colapsarContiguosIgualPrecio = true)
        {
            if (existente is null) throw new ArgumentNullException(nameof(existente));
            if (nuevo is null) throw new ArgumentNullException(nameof(nuevo));

            var lista = _tramos.ToList();
            var idx = lista.FindIndex(t => t.MinCantidad == existente.MinCantidad && t.MaxCantidad == existente.MaxCantidad);
            if (idx < 0) throw new KeyNotFoundException("El tramo a reemplazar no existe en la matriz.");

            // Validar consistencia (moneda/flag)
            if (lista.Count > 0)
            {
                var mon = lista[0].Precio.Importe.Moneda;
                var inc = lista[0].Precio.IncluyeImpuesto;
                if (nuevo.Precio.Importe.Moneda != mon)
                    throw new InvalidOperationException("Moneda del nuevo tramo difiere de la matriz.");
                if (nuevo.Precio.IncluyeImpuesto != inc)
                    throw new InvalidOperationException("IncluyeImpuesto del nuevo tramo difiere de la matriz.");
            }

            lista[idx] = nuevo;
            lista.Sort();

            ValidarSinSolapes(lista);

            if (colapsarContiguosIgualPrecio && lista.Count > 1)
                lista = ColapsarIgualesContiguos(lista);

            return new MatrizVolumen(lista);
        }

        /// <summary>
        /// Elimina un tramo por rango exacto (Min/Max). Si no existe, lanza.
        /// </summary>
        public MatrizVolumen Eliminar(TramoVolumen aQuitar)
        {
            if (aQuitar is null) throw new ArgumentNullException(nameof(aQuitar));

            var lista = _tramos.ToList();
            var removed = lista.RemoveAll(t => t.MinCantidad == aQuitar.MinCantidad && t.MaxCantidad == aQuitar.MaxCantidad);
            if (removed == 0) throw new KeyNotFoundException("El tramo a eliminar no existe en la matriz.");

            return new MatrizVolumen(lista);
        }

        // -------------------------------------------------------------------------------------
        // Igualdad / ToString
        // -------------------------------------------------------------------------------------

        public bool Equals(MatrizVolumen? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (this.Count != other.Count) return false;

            for (int i = 0; i < _tramos.Count; i++)
                if (!_tramos[i].Equals(other._tramos[i]))
                    return false;
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as MatrizVolumen);

        public override int GetHashCode()
        {
            var h = new HashCode();
            foreach (var t in _tramos) h.Add(t);
            return h.ToHashCode();
        }

        public override string ToString()
            => _tramos.Count == 0
               ? "[]"
               : string.Join(" | ", _tramos.Select(t => t.ToString()));

        // -------------------------------------------------------------------------------------
        // Validaciones internas
        // -------------------------------------------------------------------------------------

        private static void ValidarSinSolapes(List<TramoVolumen> ordenados)
        {
            for (int i = 1; i < ordenados.Count; i++)
            {
                var prev = ordenados[i - 1];
                var curr = ordenados[i];

                if (prev.SeSuperponeCon(curr))
                    throw new SharedKernel.Exceptions.BusinessRuleException($"Solape entre tramos {prev} y {curr}.");

                // No hace falta exigir contigüidad: puede haber huecos
            }
        }

        private static void ValidarConsistenciaPrecio(List<TramoVolumen> ordenados)
        {
            if (ordenados.Count <= 1) return;

            var mon = ordenados[0].Precio.Importe.Moneda;
            var inc = ordenados[0].Precio.IncluyeImpuesto;

            for (int i = 1; i < ordenados.Count; i++)
            {
                if (ordenados[i].Precio.Importe.Moneda != mon)
                    throw new InvalidOperationException("Moneda inconsistente entre tramos.");
                if (ordenados[i].Precio.IncluyeImpuesto != inc)
                    throw new InvalidOperationException("IncluyeImpuesto inconsistente entre tramos.");
            }
        }

        private static void ValidarContinuidadDesdeUno(List<TramoVolumen> ordenados)
        {
            if (ordenados.Count == 0)
                throw new InvalidOperationException("No se puede exigir continuidad con una matriz vacía.");

            if (ordenados[0].MinCantidad != 1)
                throw new InvalidOperationException("La cobertura continua debe iniciar en cantidad 1.");

            for (int i = 1; i < ordenados.Count; i++)
            {
                var prev = ordenados[i - 1];
                var curr = ordenados[i];

                if (!prev.MaxCantidad.HasValue)
                    throw new InvalidOperationException("No puede haber tramos posteriores a uno abierto (∞).");

                if (curr.MinCantidad != prev.MaxCantidad.Value + 1)
                    throw new InvalidOperationException($"Hueco entre tramos {prev} y {curr}.");
            }
        }

        private static List<TramoVolumen> ColapsarIgualesContiguos(List<TramoVolumen> ordenados)
        {
            var acc = new List<TramoVolumen>(ordenados.Count);
            TramoVolumen? last = null;

            foreach (var t in ordenados)
            {
                if (last is null)
                {
                    last = t;
                    continue;
                }

                var contiguos = last.EsContiguoCon(t);
                var mismoPrecio = last.Precio.Equals(t.Precio);

                if (contiguos && mismoPrecio)
                {
                    // Unir: [a..b] + [b+1..c] => [a..c]
                    var nuevoMax = t.MaxCantidad;
                    last = TramoVolumen.Crear(last.MinCantidad, nuevoMax, last.Precio);
                }
                else
                {
                    acc.Add(last);
                    last = t;
                }
            }

            if (last is not null) acc.Add(last);
            return acc;
        }
    }
}