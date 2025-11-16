using System;
using System.Linq;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Helpers
{
    /// <summary>
    /// Normaliza y valida la entrada proveniente de los DTOs para construir <see cref="NombrePersona"/>.
    /// Permite informar explícitamente nombres y apellidos o, en su defecto, un solo campo con ambos.
    /// </summary>
    internal static class NombrePersonaInputMapper
    {
        public static NombrePersona CrearDesdeInput(
            string? nombres,
            string? apellidos,
            string? nombresCompletos,
            string errorMessage)
        {
            if (!string.IsNullOrWhiteSpace(nombres) && !string.IsNullOrWhiteSpace(apellidos))
                return NombrePersona.Crear(nombres!, apellidos!);

            if (string.IsNullOrWhiteSpace(nombresCompletos))
                throw new BusinessRuleException(errorMessage);

            var tokens = nombresCompletos!
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < 2)
                throw new BusinessRuleException(errorMessage);

            // Heurística: si hay 3+ palabras, asumimos que los apellidos son las dos últimas.
            var apellidosCount = Math.Min(2, tokens.Length - 1);
            var nombresPartes = tokens.Take(tokens.Length - apellidosCount);
            var apellidosPartes = tokens.Skip(tokens.Length - apellidosCount);

            var nombresNormalizados = string.Join(' ', nombresPartes);
            var apellidosNormalizados = string.Join(' ', apellidosPartes);

            if (string.IsNullOrWhiteSpace(nombresNormalizados) || string.IsNullOrWhiteSpace(apellidosNormalizados))
                throw new BusinessRuleException(errorMessage);

            return NombrePersona.Crear(nombresNormalizados, apellidosNormalizados);
        }
    }
}
