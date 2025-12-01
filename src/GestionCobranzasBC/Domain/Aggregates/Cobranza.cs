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
/// Agregado raíz que representa un documento de cobranza (ej. C1 - Cobranza).
/// 
/// Modela:
/// - Identidad de la cobranza
/// - Referencia a la cuenta por cobrar
/// - Serie y número del documento
/// - Fecha, líneas de pago y distribución por cuotas
/// - Estado de la cobranza
/// - Monto total cobrado
/// </summary>
public sealed class Cobranza
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private readonly List<LineaCobro> _lineas = new();
    private readonly List<DistribucionCuota> _distribuciones = new();

    private Cobranza(
        CobranzaId id,
        CuentaPorCobrarId cuentaPorCobrarId,
        DateOnly fechaDocumento,
        string serie,
        int numero,
        CajaDestino cajaDestino,
        IEnumerable<LineaCobro> lineasCobro,
        IEnumerable<DistribucionCuota> distribucionesCuotas,
        EstadoCobranza estado,
        Dinero montoTotal,
        ToleranciaRedondeo tolerancia)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));

        CuentaPorCobrarId = cuentaPorCobrarId
            ?? throw new ArgumentNullException(nameof(cuentaPorCobrarId));

        if (string.IsNullOrWhiteSpace(serie))
        {
            throw new ArgumentException("La serie del documento de cobranza es obligatoria.", nameof(serie));
        }

        if (numero <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numero), "El número de documento debe ser mayor a cero.");
        }

        Serie = serie.Trim().ToUpperInvariant();
        Numero = numero;
        FechaDocumento = fechaDocumento;
        CajaDestino = cajaDestino ?? throw new ArgumentNullException(nameof(cajaDestino));

        if (lineasCobro is null)
        {
            throw new ArgumentNullException(nameof(lineasCobro));
        }

        var listaLineas = lineasCobro.ToList();
        if (listaLineas.Count == 0)
        {
            throw new ArgumentException("La cobranza debe tener al menos una línea de pago.", nameof(lineasCobro));
        }

        _lineas.AddRange(listaLineas);

        if (distribucionesCuotas is null)
        {
            throw new ArgumentNullException(nameof(distribucionesCuotas));
        }

        _distribuciones.AddRange(distribucionesCuotas);

        Estado = estado ?? throw new ArgumentNullException(nameof(estado));
        MontoTotal = montoTotal ?? throw new ArgumentNullException(nameof(montoTotal));
        ToleranciaRedondeo = tolerancia ?? throw new ArgumentNullException(nameof(tolerancia));

        ValidarConsistenciaMontos();
    }

    public CobranzaId Id { get; }

    public CuentaPorCobrarId CuentaPorCobrarId { get; }

    public DateOnly FechaDocumento { get; }

    public string Serie { get; }

    public int Numero { get; }

    public string NumeroCompleto => $"{Serie}-{Numero:D8}";

    public CajaDestino CajaDestino { get; }

    public IReadOnlyCollection<LineaCobro> LineasCobro => _lineas.AsReadOnly();

    /// <summary>
    /// Distribución del monto cobrado sobre las cuotas de la cuenta.
    /// </summary>
    public IReadOnlyCollection<DistribucionCuota> DistribucionesCuotas => _distribuciones.AsReadOnly();

    public EstadoCobranza Estado { get; private set; }

    public Dinero MontoTotal { get; }

    public ToleranciaRedondeo ToleranciaRedondeo { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    #region Fábrica

    /// <summary>
    /// Crea una cobranza registrada (documento C1 emitido).
    /// </summary>
    public static Cobranza CrearRegistrada(
        CobranzaId id,
        CuentaPorCobrarId cuentaPorCobrarId,
        DateOnly fechaDocumento,
        string serie,
        int numero,
        CajaDestino cajaDestino,
        IEnumerable<LineaCobro> lineasCobro,
        IEnumerable<DistribucionCuota> distribucionesCuotas,
        Dinero montoTotal,
        ToleranciaRedondeo tolerancia)
    {
        var estadoInicial = EstadoCobranza.Registrada();

        var cobranza = new Cobranza(
            id,
            cuentaPorCobrarId,
            fechaDocumento,
            serie,
            numero,
            cajaDestino,
            lineasCobro,
            distribucionesCuotas,
            estadoInicial,
            montoTotal,
            tolerancia);

        cobranza.AgregarEvento(new CobranzaRegistrada(
            cobranza.Id,
            cobranza.CuentaPorCobrarId,
            cobranza.NumeroCompleto,
            cobranza.FechaDocumento,
            cobranza.MontoTotal,
            cobranza.CajaDestino,
            cobranza.LineasCobro.ToList()));

        return cobranza;
    }

    #endregion

    #region Comportamiento

    /// <summary>
    /// Marca la cobranza como anulada por el motivo indicado.
    /// No cambia montos, solo el estado.
    /// </summary>
    public void Anular(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("Debe indicarse un motivo de anulación.", nameof(motivo));
        }

        if (Estado.EsAnulada)
        {
            return;
        }

        Estado = EstadoCobranza.Anulada(motivo);
        // Por ahora no se dispara un evento específico; se puede agregar si lo necesitas.
    }

    private void ValidarConsistenciaMontos()
    {
        // La idea es verificar que la suma de líneas coincide con el monto total
        // dentro de la tolerancia configurada.

        var sumaLineas = _lineas
            .Select(l => l.Monto)
            .Aggregate(Dinero.Cero(MontoTotal.Moneda), (acc, m) => acc + m);

        var diferencia = Math.Abs(sumaLineas.Monto - MontoTotal.Monto);

        if (diferencia > (decimal)ToleranciaRedondeo.Valor)
        {
            throw new CobranzaInvalidaException(
                $"La suma de líneas de la cobranza ({sumaLineas.Monto}) no coincide con el monto total ({MontoTotal.Monto}).");
        }

        if (_distribuciones.Count > 0)
        {
            var sumaDistribuciones = _distribuciones
                .Select(d => d.Monto)
                .Aggregate(Dinero.Cero(MontoTotal.Moneda), (acc, m) => acc + m);

            var diferenciaDistribucion = Math.Abs(sumaDistribuciones.Monto - MontoTotal.Monto);

            if (diferenciaDistribucion > (decimal)ToleranciaRedondeo.Valor)
            {
                throw new CobranzaInvalidaException(
                    $"La suma de distribuciones por cuotas ({sumaDistribuciones.Monto}) no coincide con el monto total ({MontoTotal.Monto}).");
            }
        }
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
