using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Events;

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
            // 1. Validar unicidad de SKU
            var existente = await _repo.GetProductoComboBySkuAsync(new SKU(dto.SkuCombo));
            if (existente != null)
                throw new SKUYaExisteException(dto.SkuCombo);

            // 2. Validar componentes y calcular peso total (opcional)
            var componentes = new List<ComponenteCombo>();
            decimal? pesoTotal = null;
            decimal pesoAcumulado = 0;

            // Validar componentes duplicados
            var ids = dto.Componentes.Select(c => c.ProductoServicioId).ToList();
            if (ids.Count != ids.Distinct().Count())
                throw new ComponentesDuplicadosException();

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
                    pesoAcumulado += conPeso.Peso * c.Cantidad;

                componentes.Add(new ComponenteCombo(c.ProductoServicioId, c.Cantidad));
            }

            // Si algún componente tiene peso, se asigna el peso total
            if (pesoAcumulado > 0)
                pesoTotal = pesoAcumulado;

            // 3. Crear el combo
            var combo = new ProductoCombo(
                dto.SkuCombo,
                dto.NombreCombo,
                componentes,
                dto.PrecioCombo,
                new UnidadMedida(dto.UnidadMedida),
                new AfectacionIGV(dto.AfectacionIGV),
                pesoTotal,
                dto.Estado
            );

            // 4. Publicar evento ProductoCreado (tipoProducto=COMBO)
            // combo.AgregarEvento(new ProductoCreado(combo.ProductoComboId, combo.Sku, "COMBO"));

            // 5. Persistir y confirmar
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
    public class ComponentesDuplicadosException : Exception
    {
        public ComponentesDuplicadosException() : base("No se permiten componentes repetidos en el combo.") { }
    }
}