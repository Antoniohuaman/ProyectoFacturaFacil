using System;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
	/// <summary>
	/// Snapshot de usuario emisor (vendedor, cajero, etc.) al momento de emitir el comprobante.
	/// Desacopla el comprobante del aggregate de usuario y permite auditar el estado en ese instante.
	/// </summary>
	public sealed record UsuarioSnapshot
	{
		/// <summary>Código interno del usuario (puede ser email, código, etc.).</summary>
		public string Codigo { get; init; }

		/// <summary>Nombre completo del usuario (nombre y apellidos).</summary>
		public string NombreCompleto { get; init; }

		/// <summary>Rol del usuario al momento de emitir (ej: Cajero, Vendedor, Admin).</summary>
		public string Rol { get; init; }

		public UsuarioSnapshot(string codigo, string nombreCompleto, string rol)
		{
			Codigo = string.IsNullOrWhiteSpace(codigo) ? throw new ArgumentException("El código es obligatorio.", nameof(codigo)) : codigo.Trim();
			NombreCompleto = string.IsNullOrWhiteSpace(nombreCompleto) ? throw new ArgumentException("El nombre completo es obligatorio.", nameof(nombreCompleto)) : nombreCompleto.Trim();
			Rol = string.IsNullOrWhiteSpace(rol) ? throw new ArgumentException("El rol es obligatorio.", nameof(rol)) : rol.Trim();
		}

		public override string ToString() => $"{NombreCompleto} ({Rol})";
	}
}
