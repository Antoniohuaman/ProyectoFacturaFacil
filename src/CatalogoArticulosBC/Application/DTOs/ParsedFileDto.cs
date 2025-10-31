using System.Collections.Generic;

namespace CatalogoArticulosBC.Application.DTOs
{
    public sealed class ParsedFileDto
    {
        public IReadOnlyList<string> Headers { get; init; } = new List<string>();
        public IReadOnlyCollection<Dictionary<string, string?>> Rows { get; init; } = new List<Dictionary<string, string?>>();
    }
}
