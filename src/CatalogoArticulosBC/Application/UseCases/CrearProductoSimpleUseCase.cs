using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Events;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Registra un nuevo ProductoSimple, publica ProductoCreado al final.
    /// </summary>
    public class CrearProductoSimpleUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IEventBus _eventBus;

        public CrearProductoSimpleUseCase(
            ICatalogoArticulosRepository repo,
            IUnitOfWork uow,
            IEventBus eventBus)
        {
            _repo = repo;
            _uow = uow;
            _eventBus = eventBus;
        }

        public async Task<Guid> HandleAsync(CrearProductoSimpleDto dto)
        {
            // 1. Autogenerar SKU si está vacío (Flujo alternativo)
            if (string.IsNullOrWhiteSpace(dto.Sku))
                dto.Sku = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

            // 2. Validar unicidad de SKU (RN-CA-001)
            if (await _repo.GetProductoSimpleBySkuAsync(new SKU(dto.Sku)) != null)
                throw new SKUDuplicadoException($"El SKU {dto.Sku} ya existe.");

            // 3. Validar y construir Value Objects (VOs)
            //    Si algún VO es inválido, lanzará excepción de validación correspondiente
            var unidad = new UnidadMedida(dto.UnidadMedida); // Obligatorio
            var igv = new AfectacionIGV(dto.AfectacionIgv);  // Obligatorio

            // Opcionales: solo si el DTO los trae (puedes ajustar si quieres permitir null)
            var sunat = !string.IsNullOrWhiteSpace(dto.CodigoSunat) ? new CodigoSUNAT(dto.CodigoSunat) : null;
            var baseImponible = dto.BaseImponibleVentas > 0 ? new BaseImponibleVentas(dto.BaseImponibleVentas) : null;
            var centro = !string.IsNullOrWhiteSpace(dto.CentroCosto) ? new CentroCosto(dto.CentroCosto) : null;
            var presupuesto = dto.Presupuesto > 0 ? new Presupuesto(dto.Presupuesto) : null;
            var peso = dto.Peso > 0 ? new Peso(dto.Peso) : null;
            // Estado: si lo agregas al DTO, aquí lo puedes validar también

            // 4. Crear el ProductoSimple (ajusta el constructor si acepta nulls en opcionales)
            var producto = new ProductoSimple(
                dto.Sku,
                dto.Nombre,
                dto.Descripcion,
                unidad,
                igv,
                sunat,
                baseImponible,
                centro,
                presupuesto,
                peso,
                dto.Tipo,
                dto.Precio
            );

            // 5. Persistir y commitear
            await _repo.AddProductoSimpleAsync(producto);
            await _uow.CommitAsync();

            // 6. Publicar evento ProductoCreado (con tipoProducto=SIMPLE)
            var evento = new ProductoCreado(producto.ProductoId, producto.Sku);
            await _eventBus.Publish(evento);

            // 7. Retornar el ID generado
            return producto.ProductoId;
        }
    }

    public class SKUDuplicadoException : Exception
    {
        public SKUDuplicadoException(string message) : base(message) { }
    }
}