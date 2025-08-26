using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	public class PolicyValidacionUsuarioEmisor
	{
		public class Result
		{
			public bool Success { get; }
			public string? ErrorMessage { get; }
			public IReadOnlyList<string>? ValidationErrors { get; }

			public Result(bool success, string? errorMessage = null, IReadOnlyList<string>? validationErrors = null)
			{
				Success = success;
				ErrorMessage = errorMessage;
				ValidationErrors = validationErrors;
			}
		}

		public Result Validate(object? usuarioEmisor, string? sucursalEsperada = null, string[]? permisosRequeridos = null)
		{
			var errores = new List<string>();
			if (usuarioEmisor == null)
			{
				errores.Add("El usuario emisor no puede ser nulo.");
				return new Result(false, "Usuario emisor nulo.", errores);
			}

			var tipo = usuarioEmisor.GetType();
			var activoProp = tipo.GetProperty("Activo");
			var bloqueadoProp = tipo.GetProperty("Bloqueado");
			var sucursalProp = tipo.GetProperty("Sucursal");
			var permisosProp = tipo.GetProperty("Permisos");
			var nombreProp = tipo.GetProperty("Nombre");

			// Nombre
			var nombre = nombreProp?.GetValue(usuarioEmisor) as string;
			if (string.IsNullOrWhiteSpace(nombre))
				errores.Add("El nombre del usuario emisor es obligatorio.");

			// Activo
			var activo = activoProp?.GetValue(usuarioEmisor) as bool?;
			if (activo != true)
				errores.Add("El usuario emisor debe estar activo.");

			// Bloqueado
			var bloqueado = bloqueadoProp?.GetValue(usuarioEmisor) as bool?;
			if (bloqueado == true)
				errores.Add("El usuario emisor está bloqueado y no puede emitir comprobantes.");

			// Sucursal
			var sucursal = sucursalProp?.GetValue(usuarioEmisor) as string;
			if (!string.IsNullOrWhiteSpace(sucursalEsperada) && sucursal != sucursalEsperada)
				errores.Add($"El usuario emisor debe pertenecer a la sucursal '{sucursalEsperada}'.");

			// Permisos
			if (permisosRequeridos != null && permisosRequeridos.Length > 0)
			{
				var permisos = permisosProp?.GetValue(usuarioEmisor) as IEnumerable<string>;
				foreach (var permisoReq in permisosRequeridos)
				{
					if (permisos == null || !System.Linq.Enumerable.Contains(permisos, permisoReq))
						errores.Add($"El usuario emisor no tiene el permiso requerido: '{permisoReq}'.");
				}
			}

			if (errores.Count > 0)
				return new Result(false, "Validación de usuario emisor fallida.", errores);

			return new Result(true);
		}
	}
}
