using System;
using System.Collections.Generic;
using System.Linq;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.Mappers
{
    internal static class PaqueteApplicationMapper
    {
        public static IReadOnlyCollection<ProductoPaquete.LineaProductoPaquete> ConvertirLineas(
            IEnumerable<PaqueteProductoLineaDto> lineas)
        {
            if (lineas is null)
            {
                throw new ArgumentNullException(nameof(lineas));
            }

            var resultado = new List<ProductoPaquete.LineaProductoPaquete>();

            foreach (var dto in lineas)
            {
                if (dto is null)
                {
                    throw new ArgumentException("La colección contiene una línea nula.", nameof(lineas));
                }

                var productoId = ProductoId.From(dto.ProductoId);
                var unidad = UnidadDeMedida.From(string.IsNullOrWhiteSpace(dto.UnidadMedidaCodigo)
                    ? UnidadDeMedida.NIU.Codigo
                    : dto.UnidadMedidaCodigo);
                var cantidad = CantidadProductoPaquete.Crear(dto.Cantidad);

                resultado.Add(ProductoPaquete.CrearLinea(productoId, unidad, cantidad, dto.PrecioUnitario));
            }

            return resultado;
        }

        public static IReadOnlyCollection<PaqueteProductoLineaDto> ConvertirLineasDto(
            IEnumerable<ProductoPaquete.LineaProductoPaquete> lineas)
        {
            if (lineas is null)
            {
                throw new ArgumentNullException(nameof(lineas));
            }

            return lineas
                .Select(linea => new PaqueteProductoLineaDto
                {
                    ProductoId = linea.ProductoId.Value,
                    UnidadMedidaCodigo = linea.UnidadDeMedida.Codigo,
                    Cantidad = linea.Cantidad.Valor,
                    PrecioUnitario = linea.PrecioUnitario
                })
                .ToList();
        }
    }
}
