using System;

namespace ComprobantesElectronicosBC.Application.UseCases.GuardarBorrador
{
    /// <summary>
    /// Resultado de guardar un borrador. Devuelve la identidad y lo esencial para UI.
    /// </summary>
    public sealed class GuardarBorradorOutputDto
    {
        public Guid Id { get; }
        public string Estado { get; }           // "Borrador"
        public bool EsNuevo { get; }            // true si se creó, false si se actualizó
        public string Serie { get; }
        public int? Numero { get; }

        public GuardarBorradorOutputDto(Guid id, bool esNuevo, string serie, int? numero)
        {
            Id      = id;
            EsNuevo = esNuevo;
            Estado  = "Borrador";
            Serie   = serie;
            Numero  = numero;
        }
    }
}
