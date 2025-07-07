// src/CatalogoArticulosBC/Application/DTOs/ResultadoExportacionDto.cs
namespace CatalogoArticulosBC.Application.DTOs
{
    public sealed class ResultadoExportacionDto
    {
        public ResultadoExportacionDto(byte[] contenidoExcel, string nombreArchivo)
        {
            ContenidoExcel = contenidoExcel;
            NombreArchivo  = nombreArchivo;
        }

        public byte[] ContenidoExcel { get; }
        public string NombreArchivo  { get; }
    }
}
