using System;
using System.Collections.Generic;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, Sku
using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Domain.Aggregates
{
	/// <summary>
	/// Agregado que modela el stock de un SKU en un almacén específico.
	/// Garantiza que Reservado <= Real y que ambos son >= 0.
	/// </summary>
	public sealed class StockPorAlmacen
	{
		// Identidad compuesta
		public EmpresaId EmpresaId { get; }
		public EstablecimientoId EstablecimientoId { get; }
		public AlmacenId AlmacenId { get; }
		public Sku Sku { get; }

		// Estado
		public CantidadStock Real { get; private set; }
		public CantidadStock Reservado { get; private set; }
		public CantidadStock Disponible => CantidadStock.From(Real.Value - Reservado.Value);
		public int Version { get; private set; }

		private StockPorAlmacen(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Sku sku,
			CantidadStock real, CantidadStock reservado)
		{
			EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
			EstablecimientoId = establecimientoId;
			AlmacenId = almacenId;
			Sku = sku ?? throw new ArgumentNullException(nameof(sku));
			if (reservado.Value > real.Value)
				throw new BusinessRuleException("El stock reservado no puede exceder al stock real.");
			Real = real;
			Reservado = reservado;
			Version = 0;
		}

		public static StockPorAlmacen CrearNuevo(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Sku sku)
			=> new(empresaId, establecimientoId, almacenId, sku, CantidadStock.Cero, CantidadStock.Cero);

		public void Ingresar(CantidadStock cantidad)
		{
			Real = CantidadStock.From(Real.Value + cantidad.Value);
			Version++;
		}

		public void Egresar(CantidadStock cantidad)
		{
			if (cantidad.Value > Disponible.Value)
				throw new BusinessRuleException("No hay stock disponible suficiente para egresar.");
			Real = CantidadStock.From(Real.Value - cantidad.Value);
			Version++;
		}

		public void Reservar(CantidadStock cantidad)
		{
			if (cantidad.Value > Disponible.Value)
				throw new BusinessRuleException("No hay stock disponible suficiente para reservar.");
			Reservado = CantidadStock.From(Reservado.Value + cantidad.Value);
			Version++;
		}

		public void LiberarReserva(CantidadStock cantidad)
		{
			if (cantidad.Value > Reservado.Value)
				throw new BusinessRuleException("No se puede liberar más de lo reservado.");
			Reservado = CantidadStock.From(Reservado.Value - cantidad.Value);
			Version++;
		}
	}
}

