using System;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Entities;

/// <summary>
/// Referencia al comprobante que origina la cuenta por cobrar.
/// </summary>
public sealed class DocumentoOrigen
{
    public Guid ComprobanteId { get; }
    public string Serie { get; }
    public string Numero { get; }
    public DateOnly FechaEmision { get; }
    public Moneda Moneda { get; }
    public string NumeroCompleto => $"{Serie}-{Numero}";

    private DocumentoOrigen(
        Guid comprobanteId,
        string serie,
        string numero,
        DateOnly fechaEmision,
        Moneda moneda)
    {
        ComprobanteId = comprobanteId;
        Serie = serie;
        Numero = numero;
        FechaEmision = fechaEmision;
        Moneda = moneda;
    }

    public static DocumentoOrigen Crear(
        Guid comprobanteId,
        string serie,
        string numero,
        DateOnly fechaEmision,
        Moneda moneda)
    {
        if (comprobanteId == Guid.Empty)
        {
            throw new BusinessRuleException("El identificador del comprobante no puede ser vacío.");
        }

        if (string.IsNullOrWhiteSpace(serie))
        {
            throw new BusinessRuleException("La serie del documento es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new BusinessRuleException("El número del documento es obligatorio.");
        }

        if (moneda is null)
        {
            throw new BusinessRuleException("La moneda del documento es obligatoria.");
        }

        return new DocumentoOrigen(
            comprobanteId,
            serie.Trim().ToUpperInvariant(),
            numero.Trim().PadLeft(8, '0'),
            fechaEmision,
            moneda);
    }
}
