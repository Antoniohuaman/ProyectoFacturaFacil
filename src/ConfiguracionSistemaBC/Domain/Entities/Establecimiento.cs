using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
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
	public DomicilioFiscal Direccion { get; private set; } = null!;


	public bool Habilitado { get; private set; } = true;
	public bool EsPrincipal { get; private set; } = false;

	// Relación con empresa: cada establecimiento pertenece a una empresa

		// Constructor para EF/Core y para creación
		private Establecimiento() {
			Id = null!;
			EmpresaId = null!;
		}

	public Establecimiento(EstablecimientoId id, EmpresaId empresaId, string nombre, string codigo, DomicilioFiscal direccion, bool habilitado = true, bool esPrincipal = false)
	{
		if (string.IsNullOrWhiteSpace(nombre)) throw new BusinessRuleException("El nombre es obligatorio.");
		if (string.IsNullOrWhiteSpace(codigo)) throw new BusinessRuleException("El código es obligatorio.");
		Id = id ?? throw new ArgumentNullException(nameof(id));
		EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
		Nombre = nombre;
		Codigo = codigo;
		Direccion = direccion ?? throw new ArgumentNullException(nameof(direccion));
		Habilitado = habilitado;
		EsPrincipal = esPrincipal;
	}

		// Métodos de negocio
	public void ActualizarDatos(string nombre, string codigo, DomicilioFiscal direccion)
	{
		if (string.IsNullOrWhiteSpace(nombre)) throw new BusinessRuleException("El nombre es obligatorio.");
		if (string.IsNullOrWhiteSpace(codigo)) throw new BusinessRuleException("El código es obligatorio.");
		Nombre = nombre;
		Codigo = codigo;
		Direccion = direccion ?? throw new ArgumentNullException(nameof(direccion));
	}

	public void Deshabilitar() => Habilitado = false;
	public void Habilitar() => Habilitado = true;
	public void MarcarComoPrincipal() => EsPrincipal = true;
	public void MarcarComoSecundario() => EsPrincipal = false;
	
	/// <summary>
	/// Indica si el establecimiento tiene gestiones vinculadas.
	/// Stub: Retorna false por defecto. Implementar lógica real según sea necesario.
	/// </summary>
	public bool TieneGestionesVinculadas()
	{
		// TODO: Implementar la lógica real para verificar gestiones vinculadas
		return false;
	}
	}
}
