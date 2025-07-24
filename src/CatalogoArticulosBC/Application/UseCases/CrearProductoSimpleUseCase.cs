using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Registra un nuevo ProductoSimple, publica ProductoCreado al final.
    /// </summary>
    public class CrearProductoSimpleUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public CrearProductoSimpleUseCase(
            ICatalogoArticulosRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        public async Task<Guid> HandleAsync(CrearProductoSimpleDto dto)
        {
            // 1. Autogenerar SKU si está vacío
            if (string.IsNullOrWhiteSpace(dto.Sku))
                dto.Sku = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

            // 2. Validar unicidad de SKU
            if (await _repo.GetProductoSimpleBySkuAsync(new SKU(dto.Sku)) != null)
                throw new SKUDuplicadoException($"El SKU {dto.Sku} ya existe.");  // RN-CA-001

            // 3. Construir VOs y agregado
            var unidad = new UnidadMedida(dto.UnidadMedida);
            var igv = new AfectacionIGV(dto.AfectacionIgv);
            var sunat = new CodigoSUNAT(dto.CodigoSunat);
            var baseImponible = new BaseImponibleVentas(dto.BaseImponibleVentas);
            var centro = new CentroCosto(dto.CentroCosto);
            var presupuesto = new Presupuesto(dto.Presupuesto);
            var peso = new Peso(dto.Peso);

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

            // 4. Persistir y commitear
            await _repo.AddProductoSimpleAsync(producto);
            await _uow.CommitAsync();

            return producto.ProductoId;
        }
    }

    public class SKUDuplicadoException : Exception
    {
        public SKUDuplicadoException(string message) : base(message) { }
    }
}