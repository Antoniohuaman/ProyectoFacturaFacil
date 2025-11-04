using System;
using System.Linq;
using NUnit.Framework;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Entities;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Aggregates
{
	[TestFixture]
	public class MovimientoInventarioTests
	{
		private EmpresaId E => EmpresaId.From("20123456789");
		private EstablecimientoId S => EstablecimientoId.From(Guid.NewGuid());
		private AlmacenId A => AlmacenId.New();

		[Test]
		public void Registrar_agrupa_propiedades_y_lineas()
		{
			var lineas = new[]
			{
				LineaMovimiento.Crear(Sku.Crear("SKU-1"), new CantidadStock(2m)),
				LineaMovimiento.Crear(Sku.Crear("SKU-2"), new CantidadStock(3.5m), CostoUnitario.DesdeDinero(Dinero.Create(5m, Moneda.PEN())))
			};

			var mov = MovimientoInventario.Registrar(E, S, A, DateTimeOffset.UtcNow, TipoMovimiento.Ingreso, MotivoMovimiento.Compra, lineas);
			Assert.That(mov.EmpresaId.Value, Is.EqualTo("20123456789"));
			Assert.That(mov.Lineas.Count, Is.EqualTo(2));
			Assert.That(mov.Lineas.First().Cantidad.Value, Is.EqualTo(2m));
			Assert.That(mov.Tipo, Is.EqualTo(TipoMovimiento.Ingreso));
			Assert.That(mov.Motivo, Is.EqualTo(MotivoMovimiento.Compra));
		}
	}
}

