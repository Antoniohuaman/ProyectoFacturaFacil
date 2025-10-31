using System.Collections.Generic;

namespace CatalogoArticulosBC.Application.Interfaces
{
    /// <summary>
    /// Proveedor central de esquema para importación de productos.
    /// Expone las cabeceras (ordenadas) para las plantillas "Básica" y "Completa".
    /// </summary>
    public interface IImportSchemaProvider
    {
        /// <summary>Cabeceras para plantilla básica (ordenadas, los obligatorios al inicio).</summary>
        IReadOnlyList<string> GetBasicaHeaders();

        /// <summary>Cabeceras para plantilla completa (ordenadas, los obligatorios al inicio).</summary>
        IReadOnlyList<string> GetCompletaHeaders();

        /// <summary>Lista de cabeceras mínimas obligatorias que deben aparecer al inicio de cualquier plantilla.</summary>
        IReadOnlyList<string> GetMinimumRequiredHeaders();
    }
}
