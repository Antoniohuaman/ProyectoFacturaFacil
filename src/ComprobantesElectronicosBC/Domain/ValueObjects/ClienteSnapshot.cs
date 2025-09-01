// src/ComprobantesElectronicosBC/Domain/ValueObjects/ClienteSnapshot.cs
using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Snapshot del **Cliente/Adquirente** al momento de emitir un CPE.
    /// Se persiste en el comprobante para que el PDF/XML reflejen exactamente
    /// la “foto” usada al emitir (aunque luego el maestro cambie).
    ///
    /// UBL (referencia):
    /// - <cac:AccountingCustomerParty>/<cac:Party>
    ///   - <cbc:CompanyID schemeID="{Cat.06}">Número</cbc:CompanyID>
    ///   - <cbc:RegistrationName>Nombre/Razón social</cbc:RegistrationName>
    ///   - <cac:PostalAddress>Dirección</cac:PostalAddress> (opcional)
    ///
    /// Reglas:
    /// - Documento: VO obligatorio (Catálogo 06) – la validación específica vive en DocumentoIdentidad.
    /// - Nombre/Razón social: obligatorio; se normaliza (trim + colapso de espacios).
    /// - Dirección y Email: opcionales; si existen, se pasan como VOs.
    /// </summary>
    public sealed record ClienteSnapshot
    {
        /// <summary>Identificador de la empresa emisora (multiempresa).</summary>
        public EmpresaId EmpresaId { get; init; }

        /// <summary>Identificador del tenant (multitenant).</summary>
        public TenantId TenantId { get; init; }

        /// <summary>Documento de identidad del cliente (Cat. 06).</summary>
        public DocumentoIdentidad Documento { get; init; }

        /// <summary>Nombre/Razón social del cliente (RegistrationName).</summary>
        public string Nombre { get; init; }

        /// <summary>Dirección postal del cliente (opcional).</summary>
        public DireccionPostal? Direccion { get; init; }

        /// <summary>Email de contacto del cliente (opcional).</summary>
        public Email? Email { get; init; }

        /// <summary>Código Cat.06 para RUC (útil en consultas rápidas).</summary>
        public const string SunatDocTipoRuc = "6";

        /// <summary>Conveniencia: ¿el documento del cliente es RUC?</summary>
        public bool EsRuc => Documento.EsRuc;

        // --------------------- Construcción ---------------------


        /// <summary>
        /// Constructor principal y para (de)serialización JSON. No usar directamente; preferir <see cref="Create"/>.
        /// </summary>
        [JsonConstructor]
        public ClienteSnapshot(EmpresaId empresaId, TenantId tenantId, DocumentoIdentidad documento, string nombre, DireccionPostal? direccion = null, Email? email = null)
        {
            // EmpresaId y TenantId son value objects (structs), no pueden ser null
            EmpresaId = empresaId;
            TenantId  = tenantId;
            Documento = documento ?? throw new ArgumentNullException(nameof(documento));
            Nombre    = NormalizarNombreObligatorio(nombre, nameof(nombre));
            Direccion = direccion;
            Email     = email;
        }

        /// <summary>
        /// Fábrica recomendada.
        /// </summary>
        public static ClienteSnapshot Create(EmpresaId empresaId, TenantId tenantId, DocumentoIdentidad documento, string nombre, DireccionPostal? direccion = null, Email? email = null)
        {
            // EmpresaId y TenantId son value objects (structs), no pueden ser null
            if (documento is null) throw new ArgumentNullException(nameof(documento));
            var nombreNorm = NormalizarNombreObligatorio(nombre, nameof(nombre));
            return new ClienteSnapshot(empresaId, tenantId, documento, nombreNorm, direccion, email);
        }

        // --------------------- Helpers de negocio / UBL ---------------------

        /// <summary>Valor para UBL &lt;cbc:CompanyID/@schemeID&gt; (código Cat.06).</summary>
        public string UblCompanyId_SchemeId => Documento.SchemeId;

        /// <summary>Valor para UBL &lt;cbc:CompanyID&gt; (número del documento).</summary>
        public string UblCompanyId_Value => Documento.Numero;

        /// <summary>Valor para UBL &lt;cbc:RegistrationName&gt;.</summary>
        public string UblRegistrationName => Nombre;

        public override string ToString()
            => $"{Documento} - {Nombre}";

        // --------------------- Normalización interna ---------------------

        private static readonly Regex MultiSpace = new(@"\s{2,}", RegexOptions.Compiled);

        private static string NormalizarNombreObligatorio(string? nombre, string paramName)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre/razón social del cliente es obligatorio.", paramName);

            var norm = MultiSpace.Replace(nombre.Trim(), " ");
            if (norm.Length > 150)
                throw new ArgumentException("El nombre/razón social no debe exceder 150 caracteres.", paramName);

            return norm;
        }

        // --------------------- Withers (inmutabilidad cómoda) ---------------------

        /// <summary>Devuelve un snapshot con un email distinto.</summary>
        public ClienteSnapshot ConEmail(Email? email) => this with { Email = email };

        /// <summary>Devuelve un snapshot con una dirección distinta.</summary>
        public ClienteSnapshot ConDireccion(DireccionPostal? nuevaDireccion)
            => this with { Direccion = nuevaDireccion };

        /// <summary>Devuelve un snapshot con el nombre normalizado actualizado.</summary>
        public ClienteSnapshot ConNombre(string nuevoNombre)
            => this with { Nombre = NormalizarNombreObligatorio(nuevoNombre, nameof(nuevoNombre)) };

        /// <summary>Devuelve un snapshot con empresa y tenant distintos.</summary>
        public ClienteSnapshot ConEmpresaTenant(EmpresaId nuevaEmpresaId, TenantId nuevoTenantId)
            => this with { EmpresaId = nuevaEmpresaId, TenantId = nuevoTenantId };
    }
}
