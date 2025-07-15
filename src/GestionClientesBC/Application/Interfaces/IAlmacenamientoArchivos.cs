// filepath: src/GestionClientesBC/Application/Interfaces/IAlmacenamientoArchivos.cs
using System.Threading.Tasks;
namespace GestionClientesBC.Application.Interfaces
{
    public interface IAlmacenamientoArchivos
    {
        Task<string> GuardarArchivoAsync(string nombreArchivo, byte[] archivo);
    }
}