// src/CatalogoArticulosBC/Application/DTOs/ResultadoImportacionDto.cs
namespace CatalogoArticulosBC.Application.DTOs
{
    public sealed class ResultadoImportacionDto
    {
        public ResultadoImportacionDto(
            int total,
            int exitos,
            int fallidos,
            IReadOnlyCollection<string> errores)
        {
            Total   = total;
            Exitos  = exitos;
            Fallidos= fallidos;
            Errores = errores;
        }

        public int Total   { get; }
        public int Exitos  { get; }
        public int Fallidos{ get; }
        public IReadOnlyCollection<string> Errores { get; }
    }
}
