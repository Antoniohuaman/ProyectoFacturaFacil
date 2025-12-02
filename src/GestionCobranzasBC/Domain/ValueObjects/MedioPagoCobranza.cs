using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Medio de pago SUNAT – Catálogo 59.
/// Envuelve el código (001, 002, …, 999) y su descripción.
/// </summary>
public sealed record MedioPagoCobranza
{
    private static readonly IReadOnlyDictionary<string, string> _catalogoInterno =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>
            {
                ["001"] = "Depósito en cuenta",
                ["002"] = "Giro",
                ["003"] = "Transferencia de fondos",
                ["004"] = "Orden de pago",
                ["005"] = "Tarjeta de débito",
                ["006"] = "Tarjeta de crédito emitida en el país por una empresa del sistema financiero",
                ["007"] = "Cheques con la cláusula de \"NO NEGOCIABLE\", \"INTRANSFERIBLES\", \"NO A LA ORDEN\" u otra equivalente",
                ["008"] = "Efectivo, por operaciones en las que no existe obligación de utilizar medio de pago",
                ["009"] = "Efectivo, en los demás casos",
                ["010"] = "Medios de pago usados en comercio exterior",
                ["011"] = "Documentos emitidos por las EDPYMES y las cooperativas de ahorro y crédito no autorizadas a captar depósitos del público",
                ["012"] = "Tarjeta de crédito emitida en el país o en el exterior por una empresa no perteneciente al sistema financiero",
                ["013"] = "Tarjetas de crédito emitidas en el exterior por empresas bancarias o financieras no domiciliadas",
                ["101"] = "Transferencias – Comercio exterior",
                ["102"] = "Cheques bancarios - Comercio exterior",
                ["103"] = "Orden de pago simple - Comercio exterior",
                ["104"] = "Orden de pago documentario - Comercio exterior",
                ["105"] = "Remesa simple - Comercio exterior",
                ["106"] = "Remesa documentaria - Comercio exterior",
                ["107"] = "Carta de crédito simple - Comercio exterior",
                ["108"] = "Carta de crédito documentario - Comercio exterior",
                ["999"] = "Otros medios de pago"
            });

    /// <summary>
    /// Código SUNAT de catálogo 59 (por ejemplo "008").
    /// </summary>
    public string Codigo { get; }

    /// <summary>
    /// Descripción oficial del catálogo 59.
    /// </summary>
    public string Descripcion { get; }

    private MedioPagoCobranza(string codigo, string descripcion)
    {
        Codigo = codigo;
        Descripcion = descripcion;
    }

    /// <summary>
    /// Crea un medio de pago válido a partir del código SUNAT.
    /// </summary>
    /// <exception cref="BusinessRuleException">
    /// Se lanza cuando el código no pertenece al catálogo 59.
    /// </exception>
    public static MedioPagoCobranza Crear(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new BusinessRuleException("El código de medio de pago no puede ser vacío.");
        }

        var normalizado = NormalizarCodigo(codigo);

        if (!_catalogoInterno.TryGetValue(normalizado, out var descripcion))
        {
            throw new BusinessRuleException(
                $"El código de medio de pago '{normalizado}' no pertenece al catálogo SUNAT 59.");
        }

        return new MedioPagoCobranza(normalizado, descripcion);
    }

    /// <summary>
    /// Alias semántico usado en los tests para expresar la creación desde el catálogo SUNAT.
    /// </summary>
    public static MedioPagoCobranza DesdeCodigo(string codigo) => Crear(codigo);

    /// <summary>
    /// Intenta crear un medio de pago sin lanzar excepciones.
    /// </summary>
    public static bool TryCreate(string? codigo, out MedioPagoCobranza? medioPago)
    {
        medioPago = null;

        if (string.IsNullOrWhiteSpace(codigo))
        {
            return false;
        }

        var normalizado = NormalizarCodigo(codigo);

        if (!_catalogoInterno.TryGetValue(normalizado, out var descripcion))
        {
            return false;
        }

        medioPago = new MedioPagoCobranza(normalizado, descripcion);
        return true;
    }

    /// <summary>
    /// Devuelve todos los medios de pago disponibles en el catálogo 59.
    /// </summary>
    public static IReadOnlyCollection<MedioPagoCobranza> ObtenerTodos()
        => _catalogoInterno
            .Select(kvp => new MedioPagoCobranza(kvp.Key, kvp.Value))
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Indica si el medio de pago es efectivo (008 o 009).
    /// </summary>
    public bool EsEfectivo =>
        Codigo is "008" or "009";

    /// <summary>
    /// Indica si el medio de pago corresponde a tarjeta (005, 006, 012, 013).
    /// </summary>
    public bool EsTarjeta =>
        Codigo is "005" or "006" or "012" or "013";

    private static string NormalizarCodigo(string codigo)
    {
        var trimmed = codigo.Trim();

        // Permite que el llamador envíe "8" o "08" y se normaliza a 3 dígitos.
        if (trimmed.Length < 3 && int.TryParse(trimmed, out var numero))
        {
            return numero.ToString("000");
        }

        return trimmed;
    }
}
