using System;
using SharedKernel.Exceptions; // BusinessRuleException

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Estado del cliente (habilitado/inhabilitado) como Value Object.
    /// - Inhabilitado: no se permite realizar operaciones (emitir comprobantes, etc.)
    /// - Habilitado: se permiten operaciones.
    /// Las transiciones deben hacerse en el Aggregate (Cliente), este VO solo expresa el valor/semántica.
    /// </summary>
    public sealed class EstadoCliente : IEquatable<EstadoCliente>
    {
        // Códigos canónicos (útiles para persistencia/simple serialización)
        public const string CodigoHabilitado = "HAB";
        public const string CodigoInhabilitado = "INH";

        public string Codigo { get; }   // "HAB" | "INH"
        public string Nombre { get; }   // "Habilitado" | "Inhabilitado"

        public bool EsHabilitado => Codigo == CodigoHabilitado;
        public bool PermiteOperaciones => EsHabilitado;

        // Instancias singleton (VOs por identidad de valor)
        public static readonly EstadoCliente Habilitado   = new EstadoCliente(CodigoHabilitado, "Habilitado");
        public static readonly EstadoCliente Inhabilitado = new EstadoCliente(CodigoInhabilitado, "Inhabilitado");

        // Para EF Core
        private EstadoCliente() { Codigo = null!; Nombre = null!; }

        private EstadoCliente(string codigo, string nombre)
        {
            Codigo = codigo;
            Nombre = nombre;
        }

        /// <summary>
        /// Crea el estado desde su código ("HAB", "INH"). Ignora mayúsculas/minúsculas y espacios.
        /// </summary>
        public static EstadoCliente DesdeCodigo(string codigo)
        {
            if (codigo is null) throw new BusinessRuleException("El código de estado no puede ser nulo.");

            var norm = codigo.Trim().ToUpperInvariant();
            return norm switch
            {
                CodigoHabilitado   => Habilitado,
                CodigoInhabilitado => Inhabilitado,
                _ => throw new BusinessRuleException($"Código de estado inválido: '{codigo}'. Valores permitidos: {CodigoHabilitado}, {CodigoInhabilitado}.")
            };
        }

        /// <summary>
        /// Crea el estado desde un booleano (true = Habilitado, false = Inhabilitado).
        /// </summary>
        public static EstadoCliente DesdeBool(bool habilitado) => habilitado ? Habilitado : Inhabilitado;

        /// <summary>
        /// Lanza BusinessRuleException si el estado actual no permite la operación indicada.
        /// Úsalo como guard en servicios/casos de uso.
        /// </summary>
        public void AsegurarOperacionPermitida(string operacion = "operación")
        {
            if (!PermiteOperaciones)
                throw new BusinessRuleException($"El cliente está inhabilitado: no se puede realizar la {operacion}.");
        }

        public override string ToString() => Nombre;

        #region Igualdad por valor
        public bool Equals(EstadoCliente? other) => other is not null && Codigo == other.Codigo;
        public override bool Equals(object? obj) => obj is EstadoCliente e && Equals(e);
        public override int GetHashCode() => Codigo.GetHashCode(StringComparison.Ordinal);
        #endregion
    }
}
