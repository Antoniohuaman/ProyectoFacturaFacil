using System;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.Entities;

/// <summary>
/// Referencia al comprobante que origina la cuenta por cobrar.
/// No recalcula impuestos ni lógica tributaria; solo conserva datos clave.
/// </summary>
public sealed class DocumentoOrigen
{
    public string ComprobanteId { get; private set; }
    public string TipoDocumento { get; private set; }  // Ej. "01" Factura, "03" Boleta
    public string Serie { get; private set; }
    public string Numero { get; private set; }
    public DateOnly FechaEmision { get; private set; }
    public string MonedaCodigo { get; private set; }   // Ej. "PEN", "USD"
    public decimal ImporteTotal { get; private set; }

    private DocumentoOrigen(
        string comprobanteId,
        string tipoDocumento,
        string serie,
        string numero,
        DateOnly fechaEmision,
        string monedaCodigo,
        decimal importeTotal)
    {
        ComprobanteId = comprobanteId;
        TipoDocumento = tipoDocumento;
        Serie = serie;
        Numero = numero;
        FechaEmision = fechaEmision;
        MonedaCodigo = monedaCodigo;
        ImporteTotal = importeTotal;
    }

    public static DocumentoOrigen Crear(
        string comprobanteId,
        string tipoDocumento,
        string serie,
        string numero,
        DateOnly fechaEmision,
        string monedaCodigo,
        decimal importeTotal)
    {
        if (string.IsNullOrWhiteSpace(comprobanteId))
        {
            throw new BusinessRuleException("El identificador del comprobante es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(tipoDocumento))
        {
            throw new BusinessRuleException("El tipo de documento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(serie))
        {
            throw new BusinessRuleException("La serie del documento es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new BusinessRuleException("El número del documento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(monedaCodigo))
        {
            throw new BusinessRuleException("El código de moneda es obligatorio.");
        }

        if (importeTotal <= 0m)
        {
            throw new BusinessRuleException("El importe total del documento debe ser mayor que cero.");
        }

        return new DocumentoOrigen(
            comprobanteId.Trim(),
            tipoDocumento.Trim(),
            serie.Trim(),
            numero.Trim(),
            fechaEmision,
            monedaCodigo.Trim().ToUpperInvariant(),
            importeTotal);
    }
}
