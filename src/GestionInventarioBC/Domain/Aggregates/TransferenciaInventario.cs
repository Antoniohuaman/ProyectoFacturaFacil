using System;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, ProductoId
using GestionInventarioBC.Domain.ValueObjects; // CantidadStock, EstadoTransferencia

namespace GestionInventarioBC.Domain.Aggregates
{
	/// <summary>
	/// Transferencia de stock entre almacenes (posiblemente de diferentes establecimientos) dentro de la misma empresa.
	/// </summary>
	public sealed class TransferenciaInventario
	{
		public Guid TransferenciaId { get; }
		public EmpresaId EmpresaId { get; }
		public EstablecimientoId OrigenEstablecimientoId { get; }
		public AlmacenId OrigenAlmacenId { get; }
		public EstablecimientoId DestinoEstablecimientoId { get; }
		public AlmacenId DestinoAlmacenId { get; }
		public ProductoId ProductoId { get; }
		public CantidadStock Cantidad { get; }
		public EstadoTransferencia Estado { get; private set; }
		public DateTimeOffset CreadoEn { get; }

		private TransferenciaInventario(
			Guid id,
			EmpresaId empresaId,
			EstablecimientoId origenEst,
			AlmacenId origenAlm,
			EstablecimientoId destinoEst,
			AlmacenId destinoAlm,
			ProductoId productoId,
			CantidadStock cantidad)
		{
			if (id == Guid.Empty) throw new ArgumentException("Id inválido.", nameof(id));
			if (origenEst == destinoEst && origenAlm == destinoAlm)
				throw new BusinessRuleException("El origen y destino de la transferencia no pueden ser iguales.");

			TransferenciaId = id;
			EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
			OrigenEstablecimientoId = origenEst;
			OrigenAlmacenId = origenAlm;
			DestinoEstablecimientoId = destinoEst;
			DestinoAlmacenId = destinoAlm;
			ProductoId = productoId;
			Cantidad = cantidad;
			Estado = EstadoTransferencia.Creada;
			CreadoEn = DateTimeOffset.UtcNow;
		}

		public static TransferenciaInventario Crear(
			EmpresaId empresaId,
			EstablecimientoId origenEst,
			AlmacenId origenAlm,
			EstablecimientoId destinoEst,
			AlmacenId destinoAlm,
			ProductoId productoId,
			CantidadStock cantidad)
			=> new(Guid.NewGuid(), empresaId, origenEst, origenAlm, destinoEst, destinoAlm, productoId, cantidad);

		public void Confirmar()
		{
			if (Estado != EstadoTransferencia.Creada)
				throw new BusinessRuleException("Solo transferencias creadas pueden confirmarse.");
			Estado = EstadoTransferencia.Confirmada;
		}

		public void Cancelar()
		{
			if (Estado == EstadoTransferencia.Confirmada)
				throw new BusinessRuleException("No se puede cancelar una transferencia confirmada.");
			if (Estado == EstadoTransferencia.Cancelada) return; // idempotente
			Estado = EstadoTransferencia.Cancelada;
		}
	}
}

