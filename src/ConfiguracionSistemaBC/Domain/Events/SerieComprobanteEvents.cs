using SharedKernel.Events;
using SharedKernel.ValueObjects;                       // EmpresaId, EstablecimientoId
using ConfiguracionSistemaBC.Domain.ValueObjects;     // TipoComprobanteCodigo, SerieCodigo, TipoOperacion, Correlativo
using System;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>Disparado cuando una serie es eliminada físicamente.</summary>
    public sealed class SerieComprobanteEliminada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public Guid SerieComprobanteId { get; }
        public EstablecimientoId EstablecimientoId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public SerieComprobanteEliminada(
            EmpresaId empresaId,
            Guid serieComprobanteId,
            EstablecimientoId establecimientoId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; SerieComprobanteId = serieComprobanteId; EstablecimientoId = establecimientoId; Tipo = tipo; Serie = serie; }
    }

    /// <summary>Creación de una nueva serie.</summary>
    public sealed class SerieComprobanteCreada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public EstablecimientoId EstablecimientoId { get; }
        public TipoOperacion TipoOperacion { get; }
        public Correlativo CorrelativoInicial { get; }
        public bool EsPorDefecto { get; }
        public bool Habilitada { get; }
        public SerieComprobanteCreada(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            EstablecimientoId establecimientoId,
            TipoOperacion tipoOperacion,
            Correlativo correlativoInicial,
            bool esPorDefecto,
            bool habilitada,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; Tipo = tipo; Serie = serie; EstablecimientoId = establecimientoId; TipoOperacion = tipoOperacion; CorrelativoInicial = correlativoInicial; EsPorDefecto = esPorDefecto; Habilitada = habilitada; }
    }

    public sealed class SerieComprobanteActualizada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public SerieComprobanteActualizada(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; Tipo = tipo; Serie = serie; }
    }

    public sealed class SerieComprobanteInhabilitada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public SerieComprobanteInhabilitada(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; Tipo = tipo; Serie = serie; }
    }

    public sealed class SerieComprobanteHabilitada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public SerieComprobanteHabilitada(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; Tipo = tipo; Serie = serie; }
    }

    public sealed class SerieComprobanteMarcadaPorDefecto : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public bool EsPorDefecto { get; }
        public SerieComprobanteMarcadaPorDefecto(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            bool esPorDefecto,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; Tipo = tipo; Serie = serie; EsPorDefecto = esPorDefecto; }
    }

    public sealed class CorrelativoReservado : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public Correlativo Correlativo { get; }
        public CorrelativoReservado(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            Correlativo correlativo,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; Tipo = tipo; Serie = serie; Correlativo = correlativo; }
    }

    public sealed class SerieUsadaPrimeraVez : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public SerieUsadaPrimeraVez(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; Tipo = tipo; Serie = serie; }
    }

    public sealed class NumeradorAjustado : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; }
        public Correlativo NuevoSiguiente { get; }
        public NumeradorAjustado(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            Correlativo nuevoSiguiente,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null) : base(eventId, occurredOnUtc)
        { EmpresaId = empresaId; Tipo = tipo; Serie = serie; NuevoSiguiente = nuevoSiguiente; }
    }
}
