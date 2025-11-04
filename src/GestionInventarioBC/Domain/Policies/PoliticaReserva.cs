using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Domain.Policies
{
	/// <summary>
	/// Política sencilla para validar reservas contra disponibilidad.
	/// </summary>
	public static class PoliticaReserva
	{
		public static bool PuedeReservar(DisponibilidadStock disp, CantidadStock cantidad)
			=> cantidad.Value <= disp.Disponible.Value;

		public static SharedKernel.Specifications.SpecificationResult Evaluar(DisponibilidadStock disp, CantidadStock cantidad)
			=> PuedeReservar(disp, cantidad)
				? SharedKernel.Specifications.SpecificationResult.Success()
				: SharedKernel.Specifications.SpecificationResult.Failure("RESERVA_NO_PERMITIDA", "Cantidad", "La cantidad solicitada excede el disponible.");
	}
}

