using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharedKernel.Exceptions;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Conjunto inmutable y ordenado de configuraciones de columnas de precio.
    /// Invariantes:
    /// - Cantidad: 1..10
    /// - IDs (P1..P10) únicos
    /// - Orden (1..10) único
    /// - Exactamente una columna Base (EsBase = true)
    /// - Al menos una Visible = true
    /// </summary>
    [DebuggerDisplay("Columnas={Count}")]
    public sealed class PlantillaColumnasPrecio : IEquatable<PlantillaColumnasPrecio>
    {
        private const int MaxColumnasManuales = 10;

        private readonly IReadOnlyList<ConfiguracionColumnaPrecio> _columnas;

        /// <summary>Columnas ordenadas por <see cref="ConfiguracionColumnaPrecio.Orden"/> ascendente.</summary>
        public IReadOnlyList<ConfiguracionColumnaPrecio> Columnas => _columnas;

        /// <summary>Cantidad de columnas.</summary>
        public int Count => _columnas.Count;

        /// <summary>Columnas globales de descuento/recargo.</summary>
        public IEnumerable<ConfiguracionColumnaPrecio> ColumnasGlobales => _columnas.Where(c => c.Tipo.EsGlobal);

        /// <summary>Columnas manuales (limite 10).</summary>
        public IEnumerable<ConfiguracionColumnaPrecio> ColumnasManuales => _columnas.Where(c => c.Tipo.EsManual);

        private PlantillaColumnasPrecio(IReadOnlyList<ConfiguracionColumnaPrecio> columnasOrdenadas)
        {
            _columnas = columnasOrdenadas;
        }

        // -------------------------------------------------------------------------------------
        // Fábricas
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Crea la plantilla a partir de una secuencia de columnas (pueden venir desordenadas).
        /// Valida todas las invariantes.
        /// </summary>
        public static PlantillaColumnasPrecio Crear(IEnumerable<ConfiguracionColumnaPrecio> columnas)
        {
            if (columnas is null) throw new ArgumentNullException(nameof(columnas));

            var lista = columnas.OrderBy(c => c).ToList(); // orden natural: Orden, luego Id

            ValidarIdsUnicos(lista);
            ValidarOrdenUnico(lista);
            ValidarBaseUnica(lista);
            ValidarLimiteColumnasManuales(lista);
            ValidarAlMenosUnaVisible(lista);

            // Normaliza a lista inmutable ordenada por Orden
            lista.Sort();
            return new PlantillaColumnasPrecio(lista);
        }

        /// <summary>Intenta crear sin lanzar excepciones.</summary>
        public static bool TryCrear(IEnumerable<ConfiguracionColumnaPrecio>? columnas, out PlantillaColumnasPrecio? plantilla)
        {
            plantilla = null;
            if (columnas is null) return false;

            try { plantilla = Crear(columnas); return true; }
            catch { return false; }
        }

        // -------------------------------------------------------------------------------------
        // Consultas
        // -------------------------------------------------------------------------------------

        public ConfiguracionColumnaPrecio Obtener(IdentificadorColumnaPrecio id)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));
            var cfg = _columnas.FirstOrDefault(c => c.Id.Equals(id));
            if (cfg is null) throw new KeyNotFoundException($"No existe columna con Id {id}.");
            return cfg;
        }

        /// <summary>Configuración marcada como Base (invariante: existe exactamente una).</summary>
        public ConfiguracionColumnaPrecio Base => _columnas.First(c => c.EsBase);

        /// <summary>Id de la columna Base (atajo para el resolver/UX).</summary>
        public IdentificadorColumnaPrecio IdColumnaBase => Base.Id;

        /// <summary>Número (1..10) de la columna Base (atajo para el resolver/UX).</summary>
        public byte NumeroColumnaBase => Base.Id.Numero;

        public bool Existe(IdentificadorColumnaPrecio id) => _columnas.Any(c => c.Id.Equals(id));

        // -------------------------------------------------------------------------------------
        // Transformaciones (devuelven NUEVA instancia)
        // -------------------------------------------------------------------------------------

        /// <summary>Renombra una columna (misma Id).</summary>
        public PlantillaColumnasPrecio Renombrar(IdentificadorColumnaPrecio id, NombreColumnaPrecio nuevoNombre)
            => Reemplazar(Obtener(id).Renombrar(nuevoNombre));

        /// <summary>Cambia el modo de una columna (Fijo | PorVolumen).</summary>
        public PlantillaColumnasPrecio CambiarModo(IdentificadorColumnaPrecio id, ModoValorizacionColumna nuevoModo)
            => Reemplazar(Obtener(id).CambiarModo(nuevoModo));

        /// <summary>
        /// Marca como Base la columna indicada y desmarca las demás.
        /// </summary>
        public PlantillaColumnasPrecio MarcarComoBase(IdentificadorColumnaPrecio id)
        {
            var actual = Obtener(id);
            var nuevaBase = actual.MarcarComoBase();

            var nuevas = _columnas
                .Select(c => c.Id.Equals(id) ? nuevaBase : c.DesmarcarComoBase())
                .OrderBy(c => c)
                .ToList();

            // Garantiza invariante de Base única
            ValidarBaseUnica(nuevas);
            return new PlantillaColumnasPrecio(nuevas);
        }

        /// <summary>Muestra una columna (Visible=true).</summary>
        public PlantillaColumnasPrecio Mostrar(IdentificadorColumnaPrecio id)
        {
            var nuevo = Obtener(id).Mostrar();
            return Reemplazar(nuevo);
        }

        /// <summary>Oculta una columna. No permite dejar la plantilla sin columnas visibles.</summary>
        public PlantillaColumnasPrecio Ocultar(IdentificadorColumnaPrecio id)
        {
            var objetivo = Obtener(id);
            if (_columnas.Count(c => c.Visible) == 1 && objetivo.Visible)
                throw new InvalidOperationException("No se puede ocultar la última columna visible.");

            var nuevo = objetivo.Ocultar();
            return Reemplazar(nuevo);
        }

        /// <summary>
        /// Cambia el orden de una columna. Si el nuevo orden está ocupado, hace <b>swap</b>.
        /// </summary>
        public PlantillaColumnasPrecio ConOrden(IdentificadorColumnaPrecio id, byte nuevoOrden)
        {
            var objetivo = Obtener(id);
            if (nuevoOrden < ConfiguracionColumnaPrecio.MinOrden || nuevoOrden > ConfiguracionColumnaPrecio.MaxOrden)
                throw new ArgumentOutOfRangeException(nameof(nuevoOrden), $"El orden debe estar entre {ConfiguracionColumnaPrecio.MinOrden} y {ConfiguracionColumnaPrecio.MaxOrden}.");

            var ocupada = _columnas.FirstOrDefault(c => c.Orden == nuevoOrden);

            var lista = _columnas.ToList();
            var idxObjetivo = lista.FindIndex(c => c.Id.Equals(id));
            lista[idxObjetivo] = objetivo.ConOrden(nuevoOrden);

            if (ocupada is not null && !ocupada.Id.Equals(id))
            {
                // swap: mover la otra al orden anterior del objetivo
                var idxOcupada = lista.FindIndex(c => c.Id.Equals(ocupada.Id));
                lista[idxOcupada] = ocupada.ConOrden(objetivo.Orden);
            }

            lista.Sort(); // reordenar por Orden
            ValidarOrdenUnico(lista);
            return new PlantillaColumnasPrecio(lista);
        }

        /// <summary>
        /// Reemplaza la configuración de una columna por Id (la Id debe existir).
        /// Mantiene las invariantes (Base única, al menos una visible, orden único, etc.)
        /// </summary>
        public PlantillaColumnasPrecio Reemplazar(ConfiguracionColumnaPrecio nueva)
        {
            if (nueva is null) throw new ArgumentNullException(nameof(nueva));

            var lista = _columnas.ToList();
            var idx = lista.FindIndex(c => c.Id.Equals(nueva.Id));
            if (idx < 0) throw new KeyNotFoundException($"No existe columna con Id {nueva.Id} para reemplazar.");

            lista[idx] = nueva;
            lista.Sort();

            ValidarOrdenUnico(lista);
            ValidarBaseUnica(lista);
            ValidarLimiteColumnasManuales(lista);
            ValidarAlMenosUnaVisible(lista);

            return new PlantillaColumnasPrecio(lista);
        }

        /// <summary>
        /// Agrega una columna nueva. Respeta límite 10 e invariantes.
        /// Lanza si el Id ya existe o si el Orden está ocupado (usa <see cref="ConOrden"/> si quieres swap).
        /// </summary>
        public PlantillaColumnasPrecio Agregar(ConfiguracionColumnaPrecio nueva)
        {
            if (nueva is null) throw new ArgumentNullException(nameof(nueva));
            if (Existe(nueva.Id))
                throw new InvalidOperationException($"Ya existe una columna con Id {nueva.Id}.");

            var lista = _columnas.ToList();
            lista.Add(nueva);
            lista.Sort();

            ValidarIdsUnicos(lista);
            ValidarOrdenUnico(lista);
            ValidarBaseUnica(lista);
            ValidarLimiteColumnasManuales(lista);
            ValidarAlMenosUnaVisible(lista);

            return new PlantillaColumnasPrecio(lista);
        }

        /// <summary>
        /// Elimina una columna por Id. No permite eliminar la columna Base.
        /// </summary>
        public PlantillaColumnasPrecio Eliminar(IdentificadorColumnaPrecio id)
        {
            var actual = Obtener(id);
            if (actual.EsBase)
                throw new InvalidOperationException("No se puede eliminar la columna Base.");

            var lista = _columnas.Where(c => !c.Id.Equals(id)).OrderBy(c => c).ToList();

            ValidarLimiteColumnasManuales(lista);
            ValidarAlMenosUnaVisible(lista); // si justo eliminamos la única visible, fallará

            return new PlantillaColumnasPrecio(lista);
        }

        // -------------------------------------------------------------------------------------
        // Igualdad / Representación
        // -------------------------------------------------------------------------------------

        public bool Equals(PlantillaColumnasPrecio? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (this.Count != other.Count) return false;

            for (int i = 0; i < _columnas.Count; i++)
                if (!_columnas[i].Equals(other._columnas[i]))
                    return false;
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as PlantillaColumnasPrecio);

        public override int GetHashCode()
        {
            var h = new HashCode();
            foreach (var c in _columnas) h.Add(c);
            return h.ToHashCode();
        }

        public override string ToString()
            => string.Join(" | ", _columnas.Select(c => c.ToString()));

        // -------------------------------------------------------------------------------------
        // Validaciones internas
        // -------------------------------------------------------------------------------------

        private static void ValidarIdsUnicos(List<ConfiguracionColumnaPrecio> cols)
        {
            if (cols.Select(c => c.Id.Numero).Distinct().Count() != cols.Count)
                throw new InvalidOperationException("Existen IDs de columna duplicados.");
        }

        private static void ValidarOrdenUnico(List<ConfiguracionColumnaPrecio> cols)
        {
            if (cols.Select(c => c.Orden).Distinct().Count() != cols.Count)
                throw new InvalidOperationException("Existen órdenes de columna duplicados.");
        }

        private static void ValidarBaseUnica(List<ConfiguracionColumnaPrecio> cols)
        {
            var baseCols = cols.Where(c => c.Tipo.EsBase).ToList();
            if (baseCols.Count != 1)
                throw new InvalidOperationException("Debe existir exactamente una columna de tipo Base.");

            if (!baseCols[0].EsBase)
                throw new InvalidOperationException("La columna de tipo Base debe estar marcada como base.");

            if (cols.Any(c => c.EsBase && !c.Tipo.EsBase))
                throw new InvalidOperationException("Solo las columnas de tipo Base pueden estar marcadas como base.");
        }

        private static void ValidarAlMenosUnaVisible(List<ConfiguracionColumnaPrecio> cols)
        {
            if (!cols.Any(c => c.Visible))
                throw new InvalidOperationException("Debe existir al menos una columna visible.");
        }

        private static void ValidarLimiteColumnasManuales(List<ConfiguracionColumnaPrecio> cols)
        {
            var manuales = cols.Count(c => c.Tipo.EsManual);
            if (manuales > MaxColumnasManuales)
                throw new BusinessRuleException($"No se pueden registrar más de {MaxColumnasManuales} columnas manuales en la plantilla.");
        }
    }
}
