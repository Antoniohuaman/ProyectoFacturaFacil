using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Application.UseCases
{
    public class CrearProductoComboUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public CrearProductoComboUseCase(ICatalogoArticulosRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<Guid> HandleAsync(CrearProductoComboDto dto)
        {
            // Validar unicidad de SKU
            var existente = await _repo.GetProductoComboBySkuAsync(new SKU(dto.SkuCombo));
            if (existente != null)
                throw new SKUYaExisteException(dto.SkuCombo);

            // Validar componentes y calcular peso total
            var componentes = new List<ComponenteCombo>();
            decimal pesoTotal = 0;

            foreach (var c in dto.Componentes)
            {
                if (c.Cantidad < 1)
                    throw new CantidadInvalidaException();

                // Buscar producto/variante/combo y validar que esté activo
                var producto = await _repo.GetByIdAsync(c.ProductoServicioId);
                if (producto == null || !producto.Activo)
                    throw new ProductoPadreInvalidoException(c.ProductoServicioId);

                // Sumar peso si existe
                if (producto is IProductoConPeso conPeso)
                    pesoTotal += conPeso.Peso * c.Cantidad;

                componentes.Add(new ComponenteCombo(c.ProductoServicioId, c.Cantidad));
            }

            // Crear el combo (pasa el SKU como string)
            var combo = new ProductoCombo(
                dto.SkuCombo, // <-- string, no SKU
                dto.NombreCombo,
                componentes,
                dto.PrecioCombo,
                new UnidadMedida(dto.UnidadMedida),
                new AfectacionIGV(dto.AfectacionIGV),
                pesoTotal,
                dto.Estado
            );

            // Si implementas eventos de dominio, puedes agregar aquí la lógica
            // combo.AgregarEvento(new ProductoCreado(combo.ProductoComboId, combo.Sku, "COMBO"));

            await _repo.AddProductoComboAsync(combo);
            await _uow.CommitAsync();

            return combo.ProductoComboId;
        }
    }

    // Excepciones específicas (puedes moverlas a un archivo de dominio si prefieres)
    public class SKUYaExisteException : Exception
    {
        public SKUYaExisteException(string sku) : base($"El SKU '{sku}' ya existe.") { }
    }
    public class CantidadInvalidaException : Exception
    {
        public CantidadInvalidaException() : base("La cantidad de un componente debe ser mayor o igual a 1.") { }
    }
    public class ProductoPadreInvalidoException : Exception
    {
        public ProductoPadreInvalidoException(Guid id) : base($"El producto/variante/combo con ID '{id}' no existe o está inactivo.") { }
    }
}