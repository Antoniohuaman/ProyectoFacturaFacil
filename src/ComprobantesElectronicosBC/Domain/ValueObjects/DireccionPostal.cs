namespace ComprobantesElectronicosBC.Domain.ValueObjects;

// 1) Inmutable y con igualdad por valor
public sealed record DireccionPostal
{
    // 2) Propiedades solo-get
    public string Ubigeo   { get; }
    public string Direccion { get; }
    public string? Departamento  { get; }
    public string? Provincia { get; }
    public string? Distrito  { get; }

    // 3) Ctor privado: obliga a usar fábricas que validan invariantes
    private DireccionPostal(string ubigeo, string linea, string? departamento, string? provincia, string? distrito)
    {
        Ubigeo   = ubigeo;
        Direccion = linea;
        Departamento = departamento;
        Provincia= provincia;
        Distrito = distrito;
    }

    // 4) Fábrica “genérica” + validación e invariantes
    public static DireccionPostal Create(
    string? ubigeo,
    string? direccion,
    string? departamento = null,
    string? provincia = null,
    string? distrito = null)
    {
    var normUbigeo = NormalizeUbigeo(ubigeo);      // “”, null → “” ; si se informa debe ser 6 dígitos
    var normDireccion  = NormalizeDireccion(direccion);        // “”, null → “-” (default negocio para DNI/RUC10)
    var dep        = TrimOrNull(departamento);
    var p          = TrimOrNull(provincia);
    var d          = TrimOrNull(distrito);

        // Invariantes mínimas del tipo
        if (normUbigeo.Length is not (0 or 6))
            throw new ArgumentException("El ubigeo debe tener 6 dígitos o quedar vacío.", nameof(ubigeo));

        // No permitir línea vacía o solo espacios
        if (string.IsNullOrWhiteSpace(direccion))
            throw new ArgumentException("La dirección no puede ser vacía o solo espacios.", nameof(direccion));

    return new DireccionPostal(normUbigeo, normDireccion, dep, p, d);
    }

    // 5) Fábrica de dominio (reglas RUC20 / RUC10 / DNI)
    public static DireccionPostal FromCliente(
        DocumentoIdentidad doc, // <- otro VO
        string? ubigeo,
        string? direccion,
    string? departamento = null,
    string? provincia = null,
    string? distrito = null)
    {
        bool esRuc = doc.Tipo == "6";
        bool esRuc20 = esRuc && doc.Numero.StartsWith("20");
        bool esRuc10 = esRuc && doc.Numero.StartsWith("10");
        bool esDni = doc.Tipo == "1";

    var normUbigeo = NormalizeUbigeo(ubigeo);
    var normDireccion  = NormalizeDireccion(direccion);

        if (esRuc20)
        {
            // Para RUC20 la dirección fiscal viene completa
            if (string.IsNullOrWhiteSpace(direccion) || normDireccion == "-")
                throw new ArgumentException("Para RUC 20 la dirección no puede ser vacía ni '-'.", nameof(direccion));
            if (normUbigeo.Length != 6)
                throw new ArgumentException("Para RUC 20 ubigeo de 6 dígitos es obligatorio.", nameof(ubigeo));
        }
        else if (esRuc10 || esDni)
        {
            // Para RUC10/DNI: si no hay dato, usamos “-” (autocompletado)
            if (string.IsNullOrWhiteSpace(direccion)) normDireccion = "-";
            // Ubigeo opcional
            if (normUbigeo.Length is not (0 or 6))
                throw new ArgumentException("Ubigeo debe ser vacío o 6 dígitos.", nameof(ubigeo));
        }
        else
        {
            // Otros documentos: todo opcional con las mismas reglas de formato
            if (normUbigeo.Length is not (0 or 6))
                throw new ArgumentException("Ubigeo debe ser vacío o 6 dígitos.", nameof(ubigeo));
            if (string.IsNullOrWhiteSpace(normDireccion)) normDireccion = "-";
        }

    return new DireccionPostal(normUbigeo, normDireccion, TrimOrNull(departamento), TrimOrNull(provincia), TrimOrNull(distrito));
    }

    // 6) Helpers privados (lógica de normalización = parte del valor)
    private static string NormalizeUbigeo(string? u)
        => string.IsNullOrWhiteSpace(u) ? string.Empty : u.Trim();

        private static string NormalizeDireccion(string? d)
        {
            var s = string.IsNullOrWhiteSpace(d) ? "-" : d.Trim();
            return s.Length > 200 ? s[..200] : s;
        }

    private static string? TrimOrNull(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // 7) Representación legible (útil para logs/PDF)
    public override string ToString() => $"{Direccion}";
}
