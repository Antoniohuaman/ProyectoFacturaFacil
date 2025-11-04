using System;
using System.Collections.Generic;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId
using GestionInventarioBC.Domain.Events;

namespace GestionInventarioBC.Domain.Aggregates
{
	/// <summary>
	/// Agregado Almacén: representa un almacén físico o lógico dentro de un establecimiento.
	/// </summary>
	public sealed class Almacen
	{
		private readonly List<IDomainEvent> _domainEvents = new();
		public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

		public EmpresaId EmpresaId { get; }
		public EstablecimientoId EstablecimientoId { get; }
		public AlmacenId AlmacenId { get; }

		public string Nombre { get; private set; }
		public bool Activo { get; private set; }
		public int Version { get; private set; }

		private Almacen(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, string nombre)
		{
			EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
			EstablecimientoId = establecimientoId;
			AlmacenId = almacenId;
			Nombre = string.IsNullOrWhiteSpace(nombre) ? throw new ArgumentException("El nombre es obligatorio.", nameof(nombre)) : nombre.Trim();
			Activo = true;
			Version = 0;

			_domainEvents.Add(new AlmacenCreado(EmpresaId, EstablecimientoId, AlmacenId, Nombre));
		}

		public static Almacen Crear(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, string nombre)
			=> new(empresaId, establecimientoId, almacenId, nombre);

		public void ActualizarNombre(string nuevoNombre)
		{
			if (string.IsNullOrWhiteSpace(nuevoNombre))
				throw new ArgumentException("El nombre es obligatorio.", nameof(nuevoNombre));
			if (string.Equals(Nombre, nuevoNombre.Trim(), StringComparison.Ordinal)) return;
			Nombre = nuevoNombre.Trim();
			Version++;
			_domainEvents.Add(new AlmacenActualizado(EmpresaId, EstablecimientoId, AlmacenId, Nombre));
		}

		public void Deshabilitar()
		{
			if (!Activo) return;
			Activo = false;
			Version++;
			_domainEvents.Add(new AlmacenDeshabilitado(EmpresaId, EstablecimientoId, AlmacenId));
		}

		public void Habilitar()
		{
			if (Activo) return;
			Activo = true;
			Version++;
			// No hay evento específico, pero podría agregarse AlmacenHabilitado si fuera necesario
		}
	}
}

