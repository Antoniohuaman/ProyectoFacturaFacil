using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando un producto se inhabilita (desactiva) en el sistema.
    /// Contiene el identificador del producto y la razón de la inhabilitación.
    /// </summary>
    public sealed class ProductoInhabilitado : DomainEvent
    {
        /// <summary>
        /// Identificador único del producto inhabilitado.
        /// </summary>
    public Guid ProductoId { get; }
    public ProductoId ProductoIdVO => new ProductoId(ProductoId);

        /// <summary>
        /// Empresa (tenant) a la que pertenece el producto.
        /// </summary>
        public EmpresaId EmpresaId { get; }

        /// <summary>
        /// Motivo o descripción de la inhabilitación.
        /// </summary>
        public string Motivo { get; }

        /// <summary>
        /// Crea un nuevo evento de ProductoInhabilitado.
        /// </summary>
        /// <param name="productoId">ID del producto que se inhabilita.</param>
        /// <param name="motivo">Razón de la inhabilitación. No puede ser nulo ni vacío.</param>
        public ProductoInhabilitado(Guid productoId, EmpresaId empresaId, string motivo, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ProductoId = productoId;
            EmpresaId = empresaId;
            Motivo = string.IsNullOrWhiteSpace(motivo)
                ? throw new ArgumentException("El motivo de inhabilitación no puede estar vacío.", nameof(motivo))
                : motivo.Trim();
        }
    }
}
