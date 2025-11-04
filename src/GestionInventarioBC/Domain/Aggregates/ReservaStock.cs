using System;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, ProductoId
using GestionInventarioBC.Domain.ValueObjects; // CantidadStock, EstadoReserva

namespace GestionInventarioBC.Domain.Aggregates
{
	/// <summary>
	/// Reserva de stock para un producto en un almacén.
	/// </summary>
	public sealed class ReservaStock
	{
		public Guid ReservaId { get; }
		public EmpresaId EmpresaId { get; }
		public EstablecimientoId EstablecimientoId { get; }
		public AlmacenId AlmacenId { get; }
		public ProductoId ProductoId { get; }

		public CantidadStock Cantidad { get; private set; }
		public EstadoReserva Estado { get; private set; }
		public DateTimeOffset CreadoEn { get; }
		public DateTimeOffset? VenceEn { get; private set; }

		private ReservaStock(Guid id, EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, ProductoId productoId, CantidadStock cantidad, DateTimeOffset? venceEn)
		{
			if (id == Guid.Empty) throw new ArgumentException("Id inválido.", nameof(id));
			ReservaId = id;
			EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
			EstablecimientoId = establecimientoId;
			AlmacenId = almacenId;
			ProductoId = productoId;
			Cantidad = cantidad;
			Estado = EstadoReserva.Pendiente;
			CreadoEn = DateTimeOffset.UtcNow;
			VenceEn = venceEn;
		}

		public static ReservaStock Crear(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, ProductoId productoId, CantidadStock cantidad, DateTimeOffset? venceEn)
			=> new(Guid.NewGuid(), empresaId, establecimientoId, almacenId, productoId, cantidad, venceEn);

		public void Confirmar()
		{
			if (Estado != EstadoReserva.Pendiente)
				throw new BusinessRuleException("Solo reservas pendientes pueden confirmarse.");
			Estado = EstadoReserva.Confirmada;
		}

		public void Liberar()
		{
			if (Estado != EstadoReserva.Pendiente)
				throw new BusinessRuleException("Solo reservas pendientes pueden liberarse.");
			Estado = EstadoReserva.Liberada;
		}

		public void Vencer()
		{
			if (Estado != EstadoReserva.Pendiente)
				throw new BusinessRuleException("Solo reservas pendientes pueden vencer.");
			Estado = EstadoReserva.Vencida;
		}

		public void Cancelar()
		{
			if (Estado == EstadoReserva.Confirmada)
				throw new BusinessRuleException("No se puede cancelar una reserva confirmada.");
			if (Estado is EstadoReserva.Liberada or EstadoReserva.Vencida or EstadoReserva.Cancelada)
				return; // Idempotencia
			Estado = EstadoReserva.Cancelada;
		}

		public void ExtenderHasta(DateTimeOffset nuevaFecha)
		{
			if (Estado != EstadoReserva.Pendiente)
				throw new BusinessRuleException("Solo reservas pendientes pueden extenderse.");
			if (nuevaFecha <= DateTimeOffset.UtcNow)
				throw new BusinessRuleException("La nueva fecha de vencimiento debe ser futura.");
			VenceEn = nuevaFecha;
		}
	}
}

