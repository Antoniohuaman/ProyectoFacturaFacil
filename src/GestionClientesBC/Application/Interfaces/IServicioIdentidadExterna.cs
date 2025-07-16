using System.Threading.Tasks;

namespace GestionClientesBC.Application.Interfaces
{
    public interface IServicioIdentidadExterna
    {
        Task<DatosIdentidadExterna?> ConsultarPorDniAsync(string dni);
        Task<DatosIdentidadExterna?> ConsultarPorRucAsync(string ruc);
    }

    public class DatosIdentidadExterna
    {
        public string? NombreCompleto { get; set; } // Para DNI
        public string? RazonSocial { get; set; }    // Para RUC
        public string? DireccionFiscal { get; set; }
        public string? Estado { get; set; }
    }
}