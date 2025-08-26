using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using System;

namespace ConfiguracionSistemaBC.Domain.Entities
{
	/// <summary>
	/// Representa un establecimiento físico o virtual de la empresa.
	/// </summary>
	public class Establecimiento
	{
	public EstablecimientoId Id { get; private set; }
	public EmpresaId EmpresaId { get; private set; }
		public string Nombre { get; private set; } = string.Empty;
		public string Codigo { get; private set; } = string.Empty;
		public DireccionPostal Direccion { get; private set; } = null!;
		public Telefono Telefono { get; private set; } = null!;
		public EmailEmpresa? Email { get; private set; }

	// Relación con empresa: cada establecimiento pertenece a una empresa

		// Constructor para EF/Core y para creación
		private Establecimiento() {
			Id = null!;
			EmpresaId = null!;
		}

		public Establecimiento(EstablecimientoId id, EmpresaId empresaId, string nombre, string codigo, DireccionPostal direccion, Telefono telefono, EmailEmpresa? email)
		{
			if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
			if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("El código es obligatorio.", nameof(codigo));
			Id = id ?? throw new ArgumentNullException(nameof(id));
			EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
			Nombre = nombre;
			Codigo = codigo;
			Direccion = direccion ?? throw new ArgumentNullException(nameof(direccion));
			Telefono = telefono ?? throw new ArgumentNullException(nameof(telefono));
			Email = email;
		}

		// Métodos de negocio
		public void ActualizarDatos(string nombre, string codigo, DireccionPostal direccion, Telefono telefono, EmailEmpresa? email)
		{
			if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
			if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("El código es obligatorio.", nameof(codigo));
			Nombre = nombre;
			Codigo = codigo;
			Direccion = direccion ?? throw new ArgumentNullException(nameof(direccion));
			Telefono = telefono ?? throw new ArgumentNullException(nameof(telefono));
			Email = email;
		}
	}
}
