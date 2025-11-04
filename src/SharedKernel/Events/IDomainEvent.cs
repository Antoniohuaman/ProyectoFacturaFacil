#nullable enable
namespace SharedKernel.Events
{
    /// <summary>
    /// Marca un evento de dominio. No impone propiedades para no acoplar
    /// a los agregados; si quieres metadatos comunes, usa <see cref="DomainEvent"/>.
    /// </summary>
    public interface IDomainEvent { }
}
