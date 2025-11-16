using System;
using System.Linq;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Consultar
{
    public sealed class ConsultarClienteOutputDto
    {
        // Identidad
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; set; } = null!;

        // Documento
        public string TipoDocumento { get; init; } = null!;
        public string NumeroDocumento { get; init; } = null!;

        // Nombre
        public string? RazonSocial { get; init; }
        public string? Nombres { get; init; }
        public string? NombreComercial { get; init; }

        // Contacto principal
        public string? Correo { get; init; }
        public string? Telefonos { get; init; }

        // Metadatos opcionales
        public string? PaginaWeb { get; init; }
        public string? Observaciones { get; init; }
        public string? FotoPerfilNombreArchivo { get; init; }
        public string? FotoPerfilUrl { get; init; }

        // Segmentación y estado
        public string? TipoClienteCodigo { get; init; } // "C","P","CP"
        public string? RolClienteCodigo { get; init; }  // "SIN","MAY","MIN","DIS","REV"
        public string? EstadoCodigo { get; init; }      // "HAB","INH"

        // Fechas
        public DateTime FechaRegistroUtc { get; init; }
        public DateTime? FechaDeshabilitacionUtc { get; init; }
        public string? MotivoDeshabilitacion { get; init; }
        public DateTime? FechaUltimaModificacionUtc { get; init; }

        // Dirección (resumen textual)
        public string? DomicilioFiscalResumen { get; init; }

        // Datos informativos de SUNAT (solo lectura)
        public DatosSunatClienteDto DatosSunat { get; init; } = DatosSunatClienteDto.Empty;

        // Colecciones
        public ContactoClienteDto[] Contactos { get; init; } = Array.Empty<ContactoClienteDto>();
        public AdjuntoClienteDto[] Adjuntos { get; init; } = Array.Empty<AdjuntoClienteDto>();

        public static ConsultarClienteOutputDto From(Cliente c, bool incluirContactos, bool incluirAdjuntos)
        {
            return new ConsultarClienteOutputDto
            {
                ClienteId = c.ClienteId,
                // EmpresaId se setea en el usecase (con el del tenant)
                TipoDocumento = c.Documento.Tipo.ToString(),
                NumeroDocumento = c.Documento.Numero,
                RazonSocial = c.RazonSocial?.Valor,
                Nombres = c.Nombres?.Completo,
                NombreComercial = c.NombreComercial?.ParaMostrar,
                Correo = c.Correo?.Value,
                Telefonos = c.Telefono?.UnirParaMostrar(),
                PaginaWeb = c.PaginaWeb?.Valor,
                Observaciones = c.Observaciones?.Valor,
                FotoPerfilNombreArchivo = c.FotoPerfil?.NombreArchivo,
                FotoPerfilUrl = c.FotoPerfil?.UrlPublica,
                TipoClienteCodigo = c.TipoCliente?.Codigo,
                RolClienteCodigo = c.RolCliente?.Codigo,
                EstadoCodigo = c.Estado?.Codigo,
                FechaRegistroUtc = c.FechaRegistro,
                FechaDeshabilitacionUtc = c.FechaDeshabilitacion,
                MotivoDeshabilitacion = c.MotivoDeshabilitacion,
                FechaUltimaModificacionUtc = c.FechaUltimaModificacion,
                DomicilioFiscalResumen = c.DomicilioFiscal?.ToString(),
                DatosSunat = DatosSunatClienteDto.From(c.DatosSunat),
                Contactos = incluirContactos
                    ? c.Contactos.Select(ContactoClienteDto.From).ToArray()
                    : Array.Empty<ContactoClienteDto>(),
                Adjuntos = incluirAdjuntos
                    ? c.Adjuntos.Select(AdjuntoClienteDto.From).ToArray()
                    : Array.Empty<AdjuntoClienteDto>()
            };
        }
    }

    public sealed class ContactoClienteDto
    {
        public Guid ContactoId { get; init; }
        public string NombreContacto { get; init; } = null!;
        public string? DocumentoIdentidad { get; init; } // "DNI 12345678" o null
        public string[] Emails { get; init; } = Array.Empty<string>();
        public string[] Telefonos { get; init; } = Array.Empty<string>();
        public string? Direccion { get; init; }
        public DateTime FechaCreacionUtc { get; init; }
        public DateTime? FechaModificacionUtc { get; init; }

        public static ContactoClienteDto From(ContactoCliente c) => new ContactoClienteDto
        {
            ContactoId = c.ContactoId,
            NombreContacto = c.NombreContacto.Completo,
            DocumentoIdentidad = c.DocumentoIdentidad?.ToString(),
            Emails = c.Emails.Select(e => e.Value).ToArray(),
            Telefonos = c.Telefonos.Select(t => t.UnirParaMostrar()).ToArray(),
            Direccion = c.Direccion,
            FechaCreacionUtc = c.FechaCreacion,
            FechaModificacionUtc = c.FechaModificacion
        };
    }

    public sealed class AdjuntoClienteDto
    {
        public Guid AdjuntoId { get; init; }
        public string NombreArchivo { get; init; } = null!;
        public string Ruta { get; init; } = null!;
        public DateTime FechaSubidaUtc { get; init; }
        public string? Comentario { get; init; }

        public static AdjuntoClienteDto From(AdjuntoCliente a) => new AdjuntoClienteDto
        {
            AdjuntoId = a.AdjuntoId,
            NombreArchivo = a.NombreArchivo,
            Ruta = a.Ruta,
            FechaSubidaUtc = a.FechaSubida,
            Comentario = a.Comentario
        };
    }

    public sealed class DatosSunatClienteDto
    {
        public static DatosSunatClienteDto Empty { get; } = new DatosSunatClienteDto();

        public string? TipoContribuyente { get; init; }
        public string? EstadoContribuyente { get; init; }
        public string? CondicionDomicilio { get; init; }
        public string? SistemaEmision { get; init; }
        public DateTime? FechaInscripcion { get; init; }
        public bool? EsEmisorElectronico { get; init; }
        public bool? EsAgenteRetencion { get; init; }
        public bool? EsAgentePercepcion { get; init; }
        public bool? EsBuenContribuyente { get; init; }
        public bool? ExceptuadaPercepcion { get; init; }
        public string[] ActividadesEconomicas { get; init; } = Array.Empty<string>();

        public static DatosSunatClienteDto From(DatosSunatCliente? datos)
        {
            if (datos is null)
                return Empty;

            return new DatosSunatClienteDto
            {
                TipoContribuyente = datos.TipoContribuyente,
                EstadoContribuyente = datos.EstadoContribuyente,
                CondicionDomicilio = datos.CondicionDomicilio,
                SistemaEmision = datos.SistemaEmision,
                FechaInscripcion = datos.FechaInscripcion,
                EsEmisorElectronico = datos.EsEmisorElectronico,
                EsAgenteRetencion = datos.EsAgenteRetencion,
                EsAgentePercepcion = datos.EsAgentePercepcion,
                EsBuenContribuyente = datos.EsBuenContribuyente,
                ExceptuadaPercepcion = datos.ExceptuadaPercepcion,
                ActividadesEconomicas = datos.ActividadesEconomicas.ToArray()
            };
        }
    }
}
