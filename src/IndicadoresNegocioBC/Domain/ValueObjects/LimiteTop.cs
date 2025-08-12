using System;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que encapsula el tamaño del "Top N" para rankings (productos, clientes, etc.).
    /// Invariantes:
    ///  - Valor >= 1
    ///  - Valor <= MaximoPermitido (para evitar operaciones/consultas excesivas).
    /// Notas:
    ///  - Si alguna consulta requiere "sin límite", el contrato de esa consulta debe permitir null
    ///    o un parámetro alterno. Este VO siempre representa un límite explícito y válido.
    /// </summary>
    public sealed record LimiteTop
    {
        /// <summary>
        /// Tope superior permitido por política (ajústalo según tus requerimientos).
        /// </summary>
        public const int MaximoPermitido = 100;

        /// <summary>Cantidad de elementos a devolver en el Top.</summary>
        public int Valor { get; }

        private LimiteTop(int valor)
        {
            if (valor < 1)
                throw new ArgumentOutOfRangeException(nameof(valor), "El límite debe ser mayor o igual a 1.");

            if (valor > MaximoPermitido)
                throw new ArgumentOutOfRangeException(nameof(valor), $"El límite no puede ser mayor a {MaximoPermitido}.");

            Valor = valor;
        }

        /// <summary>Fábrica principal.</summary>
        public static LimiteTop Crear(int valor) => new(valor);

        // --------- Instancias comunes (singletons) ---------
        public static readonly LimiteTop Top5  = new(5);
        public static readonly LimiteTop Top10 = new(10);
        public static readonly LimiteTop Top20 = new(20);
        public static readonly LimiteTop Top50 = new(50);

        /// <summary>
        /// Crea un límite aplicando un tope máximo personalizado. Si <paramref name="maximo"/> es inválido,
        /// se usa <see cref="MaximoPermitido"/>. Si <paramref name="valor"/> excede el tope, se recorta al tope.
        /// </summary>
        public static LimiteTop DesdeConTope(int valor, int? maximo = null)
        {
            var tope = (maximo.HasValue && maximo.Value >= 1) ? maximo.Value : MaximoPermitido;
            if (valor > tope) valor = tope;
            return new(valor);
        }

        public override string ToString() => $"Top {Valor}";
    }
}