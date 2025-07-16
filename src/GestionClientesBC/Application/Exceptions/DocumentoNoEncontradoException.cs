using System;

namespace GestionClientesBC.Domain.Exceptions
{
    public class DocumentoNoEncontradoException : Exception
    {
        public DocumentoNoEncontradoException(string numero)
            : base($"El documento {numero} no fue encontrado en el servicio externo.") { }
    }
}