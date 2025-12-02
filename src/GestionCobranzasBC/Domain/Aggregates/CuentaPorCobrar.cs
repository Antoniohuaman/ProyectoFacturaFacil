using System;
using System.Collections.Generic;
using System.Linq;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.Events;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Aggregates;

/// <summary>
/// Agregado raíz que representa la cuenta por cobrar generada a partir
/// de un comprobante de venta (factura, boleta, etc.).
/// 
/// No conoce detalles de impuestos ni de SUNAT; se enfoca en:
/// - Documento origen
/// - Cliente deudor
/// - Cronograma de cuotas
/// - Saldo pendiente y estado
/// - Eventos de dominio relevantes
/// </summary>
public sealed class CuentaPorCobrar
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private readonly List<CuotaCredito> _cuotas = new();

    private CuentaPorCobrar(
        TenantId tenantId,
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        CuentaPorCobrarId id,
        DocumentoOrigen documentoOrigen,
        Guid clienteId,
        IEnumerable<CuotaCredito> cuotas,
        SaldoPendiente saldo,
        EstadoCuentaPorCobrar estado,
        DateOnly fechaRegistro,
        ToleranciaRedondeo tolerancia)
    {
        if (tenantId.Value == Guid.Empty)
        {
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));
        }

        TenantId = tenantId;
        EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
        EstablecimientoId = establecimientoId;

        if (id == default)
        {
            throw new ArgumentException("El identificador de la cuenta por cobrar es obligatorio.", nameof(id));
        }

        Id = id;
        DocumentoOrigen = documentoOrigen ?? throw new ArgumentNullException(nameof(documentoOrigen));

        if (clienteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de cliente no puede ser vacío.", nameof(clienteId));
        }

        ClienteId = clienteId;

        if (cuotas is null)
        {
            throw new ArgumentNullException(nameof(cuotas));
        }

        var listaCuotas = cuotas.ToList();
        if (listaCuotas.Count == 0)
        {
            throw new ArgumentException("La cuenta por cobrar debe tener al menos una cuota.", nameof(cuotas));
        }

        _cuotas.AddRange(listaCuotas);

        Saldo = saldo ?? throw new ArgumentNullException(nameof(saldo));
        Estado = estado ?? throw new ArgumentNullException(nameof(estado));
        FechaRegistro = fechaRegistro;
        ToleranciaRedondeo = tolerancia ?? throw new ArgumentNullException(nameof(tolerancia));
    }

    public TenantId TenantId { get; }

    public EmpresaId EmpresaId { get; }

    public EstablecimientoId? EstablecimientoId { get; }

    public CuentaPorCobrarId Id { get; }

    /// <summary>Snapshot del comprobante que originó la cuenta.</summary>
    public DocumentoOrigen DocumentoOrigen { get; }

    /// <summary>Identificador del cliente deudor (refiere a GestionClientesBC).</summary>
    public Guid ClienteId { get; }

    /// <summary>Saldo total/cobrado/pendiente de la cuenta.</summary>
    public SaldoPendiente Saldo { get; private set; }

    /// <summary>Estado financiero de la cuenta (pendiente, parcial, vencido, cancelado).</summary>
    public EstadoCuentaPorCobrar Estado { get; private set; }

    /// <summary>Cuotas de crédito asociadas.</summary>
    public IReadOnlyCollection<CuotaCredito> Cuotas => _cuotas.AsReadOnly();

    public DateOnly FechaRegistro { get; }

    public DateOnly? FechaUltimaActualizacion { get; private set; }

    /// <summary>Tolerancia de redondeo aplicada a los cálculos de saldo.</summary>
    public ToleranciaRedondeo ToleranciaRedondeo { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    #region Fábrica

    /// <summary>
    /// Crea una nueva cuenta por cobrar recién emitida.
    /// El cálculo de saldo y estado inicial se realiza fuera (servicios de dominio)
    /// y se suministra como Value Objects para mantener la lógica aislada.
    /// </summary>
    public static CuentaPorCobrar CrearNueva(
        TenantId tenantId,
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        CuentaPorCobrarId id,
        DocumentoOrigen documentoOrigen,
        Guid clienteId,
        IEnumerable<CuotaCredito> cuotas,
        SaldoPendiente saldoInicial,
        EstadoCuentaPorCobrar estadoInicial,
        DateOnly fechaRegistro,
        ToleranciaRedondeo tolerancia)
    {
        var cuenta = new CuentaPorCobrar(
            tenantId,
            empresaId,
            establecimientoId,
            id,
            documentoOrigen,
            clienteId,
            cuotas,
            saldoInicial,
            estadoInicial,
            fechaRegistro,
            tolerancia);

        cuenta.AgregarEvento(new CuentaPorCobrarCreada(
            cuenta.TenantId,
            cuenta.EmpresaId,
            cuenta.EstablecimientoId,
            cuenta.Id,
            cuenta.DocumentoOrigen,
            cuenta.ClienteId,
            cuenta.Saldo,
            cuenta.Estado,
            cuenta.FechaRegistro));

        return cuenta;
    }

    #endregion

    #region Comportamiento

    /// <summary>
    /// Actualiza el saldo y estado de la cuenta después de un proceso
    /// de recálculo (por ejemplo, tras registrar una cobranza).
    /// La lógica de cálculo vive en servicios de dominio; aquí se preservan
    /// las invariantes y se disparan eventos.
    /// </summary>
    public void ActualizarEstado(
        SaldoPendiente nuevoSaldo,
        EstadoCuentaPorCobrar nuevoEstado,
        DateOnly fechaActualizacion)
    {
        if (nuevoSaldo is null) throw new ArgumentNullException(nameof(nuevoSaldo));
        if (nuevoEstado is null) throw new ArgumentNullException(nameof(nuevoEstado));

        Saldo = nuevoSaldo;
        Estado = nuevoEstado;
        FechaUltimaActualizacion = fechaActualizacion;

        AgregarEvento(new CuentaPorCobrarActualizada(
            TenantId,
            EmpresaId,
            EstablecimientoId,
            Id,
            DocumentoOrigen,
            ClienteId,
            Saldo,
            Estado,
            fechaActualizacion));

        if (Estado.EsCancelado)
        {
            AgregarEvento(new CuentaPorCobrarCancelada(
                TenantId,
                EmpresaId,
                EstablecimientoId,
                Id,
                DocumentoOrigen,
                ClienteId,
                Saldo,
                fechaActualizacion));
        }
        else if (Estado.EsVencido)
        {
            AgregarEvento(new CuentaPorCobrarVencida(
                TenantId,
                EmpresaId,
                EstablecimientoId,
                Id,
                DocumentoOrigen,
                ClienteId,
                Saldo,
                fechaActualizacion));
        }
    }

    /// <summary>
    /// Registra que se aplicó un pago (cobranza) sobre esta cuenta.
    /// La aplicación concreta sobre cuotas y el recálculo del saldo
    /// se realizan externamente; aquí solo se asegura la integridad
    /// y se publican eventos.
    /// </summary>
    public void RegistrarPagoAplicado(
        CobranzaId cobranzaId,
        SaldoPendiente saldoDespuesDePago,
        EstadoCuentaPorCobrar estadoDespuesDePago,
        DateOnly fechaAplicacion)
    {
        if (cobranzaId == default) throw new ArgumentException("El identificador de la cobranza es obligatorio.", nameof(cobranzaId));

        ActualizarEstado(saldoDespuesDePago, estadoDespuesDePago, fechaAplicacion);

        AgregarEvento(new PagoAplicadoACuota(
            TenantId,
            EmpresaId,
            EstablecimientoId,
            Id,
            cobranzaId,
            DocumentoOrigen,
            Saldo,
            Estado,
            fechaAplicacion));
    }

    public bool TieneCuotasVencidas(DateOnly fechaReferencia)
    {
        return _cuotas.Any(c => c.Saldo.Monto > 0m && c.FechaVencimiento < fechaReferencia);
    }

    #endregion

    #region DomainEvents helpers

    private void AgregarEvento(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void LimpiarEventos()
    {
        _domainEvents.Clear();
    }

    #endregion
}
