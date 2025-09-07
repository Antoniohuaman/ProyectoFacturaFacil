using System;
using System.Collections.Generic;
using System.Linq;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Application.UseCases.ConsultarProducto
{
    /// <summary>
    /// Proyección de lectura para la ficha detallada del producto.
    /// </summary>
    public sealed class ConsultarProductoOutputDto
    {
        // Contexto
        public string EmpresaId { get; init; } = string.Empty;
        public DateTimeOffset ConsultadoEnUtc { get; init; }

        // Identidad y estado
        public Guid ProductoId { get; init; }
        public bool Habilitado { get; init; }

        // Clave de negocio
        public string Sku { get; init; } = string.Empty;

        // Datos básicos
        public string Nombre { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
        public string UnidadMedida { get; init; } = string.Empty;
        public string AfectacionImpuesto { get; init; } = string.Empty;
        public decimal TasaImpuestoFraccion { get; init; }
        public string Categoria { get; init; } = string.Empty;
        public string? Marca { get; init; }

        // Precios y moneda
        public decimal? PrecioVenta { get; init; }
        public string Moneda { get; init; } = string.Empty;

        // Códigos adicionales
        public string? CodigoSunat { get; init; }
        public string? CentroDeCosto { get; init; }
        public string? CodigoBarras { get; init; }
        public string? CodigoFabrica { get; init; }

        // Peso e inventario
        public decimal Peso { get; init; }
        public string TipoExistencia { get; init; } = string.Empty;

        // Tipo de producto
        public string TipoProducto { get; init; } = string.Empty;

        // Establecimientos
        public bool AsignarATodosLosEstablecimientos { get; init; }
        public IReadOnlyCollection<string> EstablecimientosAsignados { get; init; } = Array.Empty<string>();

        // Multimedia
        public Guid? ImagenPrincipalId { get; init; }
        public IReadOnlyCollection<MultimediaDto> Multimedia { get; init; } = Array.Empty<MultimediaDto>();

        public sealed class MultimediaDto
        {
            public Guid MultimediaId { get; init; }
            public string TipoMime { get; init; } = string.Empty;
            public string TipoAdjunto { get; init; } = string.Empty;
            public string NombreArchivo { get; init; } = string.Empty;
            public string Ruta { get; init; } = string.Empty;
            public string Comentario { get; init; } = string.Empty;
            public long Tamano { get; init; }
            public DateTime FechaCarga { get; init; }
        }

        public static ConsultarProductoOutputDto From(
            ProductoSimple p,
            IReadOnlyCollection<MultimediaProducto> multimedia,
            string empresaId)
        {
            return new ConsultarProductoOutputDto
            {
                EmpresaId = empresaId,
                ConsultadoEnUtc = DateTimeOffset.UtcNow,

                // Identidad y estado
                ProductoId = p.ProductoId,
                Habilitado = p.Habilitado,

                // Clave
                Sku = p.Sku?.Valor ?? string.Empty,

                // Datos básicos
                Nombre = p.Nombre?.Valor ?? string.Empty,
                Descripcion = p.Descripcion ?? string.Empty,
                UnidadMedida = p.UnidadMedida?.ToString() ?? string.Empty,
                AfectacionImpuesto = p.AfectacionImpuesto?.ToString() ?? string.Empty,
                TasaImpuestoFraccion = p.TasaImpuesto?.Fraccion ?? 0m,
                Categoria = p.Categoria?.Nombre ?? string.Empty,
                Marca = p.Marca?.Nombre, // si tu VO Marca expone Nombre

                // Precios y moneda
                PrecioVenta = p.PrecioVenta?.Monto,
                Moneda = p.Moneda?.ToString() ?? string.Empty,

                // Códigos
                CodigoSunat = p.CodigoSunat?.ToString(),
                CentroDeCosto = p.CentroDeCosto?.ToString(),
                CodigoBarras = p.CodigoBarras?.ToString(),
                CodigoFabrica = p.CodigoFabrica?.ToString(),

                // Peso e inventario
                Peso = p.Peso?.Valor ?? 0m,
                TipoExistencia = p.TipoExistencia.ToString(),

                // Tipo
                TipoProducto = p.Tipo.ToString(),

                // Establecimientos
                AsignarATodosLosEstablecimientos = p.AsignarATodosLosEstablecimientos,
                EstablecimientosAsignados = p.EstablecimientosAsignados?
                    .Select(e => e.ToString())
                    .ToArray() ?? Array.Empty<string>(),

                // Multimedia
                ImagenPrincipalId = p.ImagenPrincipalId,
                Multimedia = multimedia?
                    .Select(m => new MultimediaDto
                    {
                        MultimediaId = m.MultimediaId,
                        TipoMime = m.TipoMime,
                        TipoAdjunto = m.TipoAdjunto,
                        NombreArchivo = m.NombreArchivo,
                        Ruta = m.Ruta,
                        Comentario = m.Comentario,
                        Tamano = m.Tamano,
                        FechaCarga = m.FechaCarga
                    })
                    .ToArray() ?? Array.Empty<MultimediaDto>()
            };
        }
    }
}
