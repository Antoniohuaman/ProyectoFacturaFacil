using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Parámetros para consultar la configuración general de una empresa.
    /// Por defecto consulta la empresa del contexto (ITenantContext.EmpresaId).
    /// </summary>
    public sealed class ConsultarConfiguracionGeneralInputDto
    {
        /// <summary>
        /// (Opcional) EmpresaId explícito (GUID-string). Si no se envía, se usa el del contexto.
        /// </summary>
        public string? EmpresaId { get; init; }

        /// <summary>Incluir el listado de establecimientos.</summary>
        public bool IncluirEstablecimientos { get; init; } = true;

        /// <summary>Incluir catálogos locales: Formas de pago y Unidades de medida.</summary>
        public bool IncluirCatalogos { get; init; } = true;

        /// <summary>
        /// Si es false, oculta elementos no visibles (Visible == false) en catálogos.
        /// Solo aplica a catálogos (formas de pago / unidades).
        /// </summary>
        public bool IncluirOcultos { get; init; } = false;
    }
}
