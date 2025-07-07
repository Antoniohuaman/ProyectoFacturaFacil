// src/CatalogoArticulosBC/Application/UseCases/CrearProductoSimpleUseCase.cs
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
            // 1. Validar unicidad de SKU
            if (await _repo.GetProductoSimpleBySkuAsync(new SKU(dto.Sku)) != null)
                throw new InvalidOperationException($"El SKU {dto.Sku} ya existe.");  // RN-CA-001 :contentReference[oaicite:0]{index=0}

            // 2. Construir VOs y agregado
            var unidad    = new UnidadMedida(dto.UnidadMedida);
            var igv       = new AfectacionIGV(dto.AfectacionIgv);
            var sunat     = new CodigoSUNAT(dto.CodigoSunat);
            var cuenta    = new CuentaContable(dto.CuentaContable);
            var centro    = new CentroCosto(dto.CentroCosto);
            var presupuesto = new Presupuesto(dto.Presupuesto);
            var peso      = new Peso(dto.Peso);

            var producto = new ProductoSimple(
                dto.Sku,
                dto.Nombre,
                dto.Descripcion,
                unidad,
                igv,
                sunat,
                cuenta,
                centro,
                presupuesto,
                peso);

            // 3. Persistir y commitear
            await _repo.AddProductoSimpleAsync(producto);
            await _uow.CommitAsync();

            return producto.ProductoId;
        }
    }
}
