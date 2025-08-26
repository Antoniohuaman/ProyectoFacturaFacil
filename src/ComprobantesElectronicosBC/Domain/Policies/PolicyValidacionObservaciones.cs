using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	public class PolicyValidacionObservaciones
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

		public Result Validate(string? observacion, int maxCaracteres = 250, bool obligatorio = false, string[]? palabrasProhibidas = null)
		{
			var errores = new List<string>();

			if (string.IsNullOrWhiteSpace(observacion))
			{
				if (obligatorio)
					errores.Add("La observación es obligatoria y no puede estar vacía.");
			}
			else
			{
				if (observacion.Length > maxCaracteres)
					errores.Add($"La observación excede el máximo de {maxCaracteres} caracteres.");

				// Validación de caracteres inválidos (solo texto y algunos signos permitidos)
				if (!Regex.IsMatch(observacion, @"^[\w\s.,;:¡!¿?\-()áéíóúÁÉÍÓÚñÑ]*$"))
					errores.Add("La observación contiene caracteres inválidos.");

				// Palabras prohibidas
				if (palabrasProhibidas != null)
				{
					foreach (var palabra in palabrasProhibidas)
					{
						if (!string.IsNullOrWhiteSpace(palabra) && observacion.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0)
							errores.Add($"La observación contiene la palabra prohibida: '{palabra}'.");
					}
				}
			}

			if (errores.Count > 0)
				return new Result(false, "Validación de observaciones fallida.", errores);

			return new Result(true);
		}
	}
}
