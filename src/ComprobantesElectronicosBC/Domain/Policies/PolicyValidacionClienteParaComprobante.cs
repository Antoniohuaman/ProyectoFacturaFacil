using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	public class PolicyValidacionClienteParaComprobante
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

		public Result Validate(object? cliente)
		{
			var errors = new List<string>();
			if (cliente == null)
			{
				errors.Add("El cliente no puede ser nulo.");
				return new Result(false, "Cliente nulo.", errors);
			}

			// Reflection: Try to get required properties
			var tipo = cliente.GetType();
			var nombreProp = tipo.GetProperty("Nombre");
			var activoProp = tipo.GetProperty("Activo");
			var rucProp = tipo.GetProperty("RUC");
			var direccionProp = tipo.GetProperty("Direccion");
			var bloqueadoProp = tipo.GetProperty("Bloqueado");
			var deudaVencidaProp = tipo.GetProperty("DeudaVencida");

			// Nombre
			var nombre = nombreProp?.GetValue(cliente) as string;
			if (string.IsNullOrWhiteSpace(nombre))
				errors.Add("El nombre del cliente es obligatorio.");

			// Activo
			var activo = activoProp?.GetValue(cliente) as bool?;
			if (activo != true)
				errors.Add("El cliente debe estar activo.");

			// RUC
			var ruc = rucProp?.GetValue(cliente) as string;
			if (string.IsNullOrWhiteSpace(ruc))
				errors.Add("El RUC/DNI del cliente es obligatorio.");

			// Dirección
			var direccion = direccionProp?.GetValue(cliente) as string;
			if (string.IsNullOrWhiteSpace(direccion))
				errors.Add("La dirección del cliente es obligatoria.");

			// Bloqueado
			var bloqueado = bloqueadoProp?.GetValue(cliente) as bool?;
			if (bloqueado == true)
				errors.Add("El cliente está bloqueado y no puede recibir comprobantes.");

			// Deuda vencida
			var deudaVencida = deudaVencidaProp?.GetValue(cliente) as bool?;
			if (deudaVencida == true)
				errors.Add("El cliente tiene deuda vencida y no puede recibir comprobantes.");

			if (errors.Count > 0)
				return new Result(false, "Validación de cliente fallida.", errors);

			return new Result(true);
		}
	}
}
