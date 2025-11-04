using System;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, Sku
using GestionInventarioBC.Domain.ValueObjects; // CantidadStock, EstadoReserva

namespace GestionInventarioBC.Domain.Aggregates
{
	/// <summary>
	/// Reserva de stock para un SKU en un almacén.
	/// </summary>
	public sealed class ReservaStock
	{
		public Guid ReservaId { get; }
		public EmpresaId EmpresaId { get; }
		public EstablecimientoId EstablecimientoId { get; }
		public AlmacenId AlmacenId { get; }
		public Sku Sku { get; }

		public CantidadStock Cantidad { get; private set; }
		public EstadoReserva Estado { get; private set; }
		public DateTimeOffset CreadoEn { get; }
		public DateTimeOffset? VenceEn { get; private set; }

		private ReservaStock(Guid id, EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Sku sku, CantidadStock cantidad, DateTimeOffset? venceEn)
		{
			if (id == Guid.Empty) throw new ArgumentException("Id inválido.", nameof(id));
			ReservaId = id;
			EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
			EstablecimientoId = establecimientoId;
			AlmacenId = almacenId;
			Sku = sku ?? throw new ArgumentNullException(nameof(sku));
			Cantidad = cantidad;
			Estado = EstadoReserva.Pendiente;
			CreadoEn = DateTimeOffset.UtcNow;
			VenceEn = venceEn;
		}

		public static ReservaStock Crear(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Sku sku, CantidadStock cantidad, DateTimeOffset? venceEn)
			=> new(Guid.NewGuid(), empresaId, establecimientoId, almacenId, sku, cantidad, venceEn);

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
	}
}

