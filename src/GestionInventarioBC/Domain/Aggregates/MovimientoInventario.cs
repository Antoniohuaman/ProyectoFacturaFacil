using System;
using System.Collections.Generic;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, Sku
using GestionInventarioBC.Domain.Entities; // LineaMovimiento
using GestionInventarioBC.Domain.ValueObjects; // TipoMovimiento, MotivoMovimiento

namespace GestionInventarioBC.Domain.Aggregates
{
	/// <summary>
	/// Movimiento de inventario (ingreso/egreso/ajuste/transferencia) agrupando líneas por SKU.
	/// </summary>
	public sealed class MovimientoInventario
	{
		public Guid MovimientoId { get; }
		public EmpresaId EmpresaId { get; }
		public EstablecimientoId EstablecimientoId { get; }
		public AlmacenId AlmacenId { get; }

		public DateTimeOffset Fecha { get; }
		public ValueObjects.TipoMovimiento Tipo { get; }
		public ValueObjects.MotivoMovimiento Motivo { get; }

		private readonly List<LineaMovimiento> _lineas = new();
		public IReadOnlyCollection<LineaMovimiento> Lineas => _lineas.AsReadOnly();

		private MovimientoInventario(
			Guid id,
			EmpresaId empresaId,
			EstablecimientoId establecimientoId,
			AlmacenId almacenId,
			DateTimeOffset fecha,
			ValueObjects.TipoMovimiento tipo,
			ValueObjects.MotivoMovimiento motivo,
			IEnumerable<LineaMovimiento> lineas)
		{
			MovimientoId = id == Guid.Empty ? Guid.NewGuid() : id;
			EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
			EstablecimientoId = establecimientoId;
			AlmacenId = almacenId;
			Fecha = fecha;
			Tipo = tipo;
			Motivo = motivo;
			if (lineas is null) throw new ArgumentNullException(nameof(lineas));
			_lineas.AddRange(lineas);
		}

		public static MovimientoInventario Registrar(
			EmpresaId empresaId,
			EstablecimientoId establecimientoId,
			AlmacenId almacenId,
			DateTimeOffset fecha,
			ValueObjects.TipoMovimiento tipo,
			ValueObjects.MotivoMovimiento motivo,
			IEnumerable<LineaMovimiento> lineas)
			=> new(Guid.NewGuid(), empresaId, establecimientoId, almacenId, fecha, tipo, motivo, lineas);
	}
}

