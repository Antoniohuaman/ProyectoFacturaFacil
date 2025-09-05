using SharedKernel.Events;
using SharedKernel.ValueObjects;                       // EmpresaId, EstablecimientoId
using ConfiguracionSistemaBC.Domain.ValueObjects;     // TipoComprobanteCodigo, SerieCodigo, TipoOperacion, Correlativo

namespace ConfiguracionSistemaBC.Domain.Events
{
    // Creación / cambios de configuración
    public sealed record SerieComprobanteCreada(
        EmpresaId EmpresaId,
        TipoComprobanteCodigo Tipo,
        SerieCodigo Serie,
        EstablecimientoId EstablecimientoId,
        TipoOperacion TipoOperacion,
        Correlativo CorrelativoInicial,
        bool EsPorDefecto,
        bool Habilitada) : IDomainEvent;

    public sealed record SerieComprobanteActualizada(
        EmpresaId EmpresaId,
        TipoComprobanteCodigo Tipo,
        SerieCodigo Serie) : IDomainEvent;

    public sealed record SerieComprobanteInhabilitada(
        EmpresaId EmpresaId,
        TipoComprobanteCodigo Tipo,
        SerieCodigo Serie) : IDomainEvent;

    public sealed record SerieComprobanteHabilitada(
        EmpresaId EmpresaId,
        TipoComprobanteCodigo Tipo,
        SerieCodigo Serie) : IDomainEvent;

    public sealed record SerieComprobanteMarcadaPorDefecto(
        EmpresaId EmpresaId,
        TipoComprobanteCodigo Tipo,
        SerieCodigo Serie,
        bool EsPorDefecto) : IDomainEvent;

    // Numeración
    public sealed record CorrelativoReservado(
        EmpresaId EmpresaId,
        TipoComprobanteCodigo Tipo,
        SerieCodigo Serie,
        Correlativo Correlativo) : IDomainEvent;

    public sealed record SerieUsadaPrimeraVez(
        EmpresaId EmpresaId,
        TipoComprobanteCodigo Tipo,
        SerieCodigo Serie) : IDomainEvent;

    public sealed record NumeradorAjustado(
        EmpresaId EmpresaId,
        TipoComprobanteCodigo Tipo,
        SerieCodigo Serie,
        Correlativo NuevoSiguiente) : IDomainEvent;
}
