using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Snapshot del Emisor al momento de emitir un CPE (se persiste en el documento).
    /// </summary>
    // Email proviene de SharedKernel.ValueObjects
    public sealed record EmisorSnapshot
    {
        /// <summary>Identificador de la empresa (multiempresa).</summary>
        public EmpresaId EmpresaId { get; init; }

        /// <summary>Identificador del tenant (multitenant).</summary>
        public TenantId TenantId { get; init; }

        /// <summary>Identificador del establecimiento.</summary>
        public EstablecimientoId EstablecimientoId { get; init; }
        /// <summary>RUC de 11 dígitos.</summary>
        public string Ruc { get; init; }

        /// <summary>Razón social (RegistrationName).</summary>
        public string RazonSocial { get; init; }

        /// <summary>Nombre comercial (opcional).</summary>
        public string? NombreComercial { get; init; }

        /// <summary>Dirección postal del emisor.</summary>
        public DireccionPostal Direccion { get; init; }

        /// <summary>Email de contacto del emisor (opcional).</summary>
        public Email? Email { get; init; }

        /// <summary>Teléfono(s) de contacto del emisor (opcional).</summary>
        public Telefono? Telefono { get; init; }

        public const string SunatDocTipoRuc = "6";


        private EmisorSnapshot(
            EmpresaId empresaId,
            TenantId tenantId,
            EstablecimientoId establecimientoId,
            string ruc,
            string razonSocial,
            DireccionPostal direccion,
            string? nombreComercial,
            Email? email,
            Telefono? telefono)
        {
            EmpresaId = empresaId;
            TenantId = tenantId;
            EstablecimientoId = establecimientoId;
            Ruc = ruc;
            RazonSocial = razonSocial;
            Direccion = direccion;
            NombreComercial = nombreComercial;
            Email = email;
            Telefono = telefono;
        }


        [JsonConstructor]
        public EmisorSnapshot(
            EmpresaId empresaId,
            TenantId tenantId,
            EstablecimientoId establecimientoId,
            string ruc,
            string razonSocial,
            DireccionPostal direccion,
            string? nombreComercial = null)
            : this(
                empresaId,
                tenantId,
                establecimientoId,
                NormalizarRuc(ruc),
                NormalizarNombreObligatorio(razonSocial, nameof(razonSocial)),
                direccion ?? throw new ArgumentNullException(nameof(direccion)),
                NormalizarNombreOpcional(nombreComercial),
                null,
                null)
        { }


        public static EmisorSnapshot Create(
            EmpresaId empresaId,
            TenantId tenantId,
            EstablecimientoId establecimientoId,
            string ruc,
            string razonSocial,
            DireccionPostal direccion,
            string? nombreComercial = null,
            Email? email = null,
            Telefono? telefono = null)
        {
            var rucNorm = NormalizarRuc(ruc);
            var razonNorm = NormalizarNombreObligatorio(razonSocial, nameof(razonSocial));
            var nombreCom = NormalizarNombreOpcional(nombreComercial);
            return new EmisorSnapshot(
                empresaId,
                tenantId,
                establecimientoId,
                rucNorm,
                razonNorm,
                direccion ?? throw new ArgumentNullException(nameof(direccion)),
                nombreCom,
                email,
                telefono);
        }

        // ------- Helpers UBL -------
        public string UblCompanyId_SchemeId => SunatDocTipoRuc;
        public string UblCompanyId_Value    => Ruc;
        public string UblRegistrationName   => RazonSocial;
        public string? UblCommercialName    => NombreComercial;

        public override string ToString()
            => NombreComercial is null ? $"{Ruc} - {RazonSocial}" : $"{Ruc} - {RazonSocial} (\"{NombreComercial}\")";

        // ------- Normalización / Validación interna -------
        private const int RucLength = 11;
        private static readonly string[] RucPrefixesPermitidos = { "10", "15", "16", "17", "20" };
        private static readonly Regex MultiSpace = new(@"\s{2,}", RegexOptions.Compiled);

        private static string NormalizarRuc(string? ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc))
                throw new ArgumentException("El RUC es obligatorio.", nameof(ruc));

            var digits = Regex.Replace(ruc, @"\D", "");
            if (digits.Length != RucLength)
                throw new ArgumentException("El RUC debe tener 11 dígitos.", nameof(ruc));

            var prefix = digits[..2];
            var prefOk = Array.Exists(RucPrefixesPermitidos, p => p == prefix);
            if (!prefOk)
                throw new ArgumentException($"Prefijo de RUC inválido ({prefix}). Se esperan 10/15/16/17/20.", nameof(ruc));

            return digits;
        }

        private static string NormalizarNombreObligatorio(string? nombre, string paramName)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre/razón social es obligatorio.", paramName);

            var norm = MultiSpace.Replace(nombre.Trim(), " ");
            if (norm.Length > 150)
                throw new ArgumentException("El nombre/razón social no debe exceder 150 caracteres.", paramName);

            return norm;
        }

        private static string? NormalizarNombreOpcional(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            var norm = MultiSpace.Replace(nombre.Trim(), " ");
            return norm.Length == 0 ? null : (norm.Length > 150 ? norm[..150] : norm);
        }

        // ------- Withers (crean nuevo snapshot) -------
        public EmisorSnapshot ConEmail(Email? email) => this with { Email = email };
        public EmisorSnapshot ConTelefono(Telefono? telefono) => this with { Telefono = telefono };
        public EmisorSnapshot ConDireccion(DireccionPostal nuevaDireccion)
            => this with { Direccion = nuevaDireccion ?? throw new ArgumentNullException(nameof(nuevaDireccion)) };
        public EmisorSnapshot ConNombreComercial(string? nuevoNombreComercial)
            => this with { NombreComercial = NormalizarNombreOpcional(nuevoNombreComercial) };
    }
}
