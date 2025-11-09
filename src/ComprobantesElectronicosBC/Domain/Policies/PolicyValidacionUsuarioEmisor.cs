using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// [OBSOLETO] Reemplazado por invariantes del agregado y snapshots fuertes.
	/// Eliminado del flujo: no se utiliza en runtime y usaba reflection costoso.
	/// Conservado como marcador temporal para permitir revisión histórica.
	/// </summary>
	[Obsolete("Eliminar en próxima limpieza: validaciones de usuario emisor migradas a creación de UsuarioSnapshot/EmisorSnapshot.")]
	public class PolicyValidacionUsuarioEmisor { }
}
