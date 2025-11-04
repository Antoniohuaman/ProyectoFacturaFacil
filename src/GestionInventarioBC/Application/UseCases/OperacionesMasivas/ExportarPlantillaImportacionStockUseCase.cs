using System.Threading;
using System.Threading.Tasks;

namespace GestionInventarioBC.Application.UseCases.OperacionesMasivas
{
	/// <summary>
	/// Devuelve una plantilla CSV mínima para importación de stock (cabeceras).
	/// </summary>
	public sealed class ExportarPlantillaImportacionStockUseCase
	{
		public readonly record struct Request();
		public readonly record struct Response(string CsvContenido);

		public Task<Response> Handle(Request _, CancellationToken __)
		{
			// Cabeceras separadas por ';' para compatibilidad regional
			const string plantilla = "SKU;CANTIDAD";
			return Task.FromResult(new Response(plantilla));
		}
	}
}

