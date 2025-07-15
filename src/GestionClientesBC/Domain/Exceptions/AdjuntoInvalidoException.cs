using System;

namespace GestionClientesBC.Domain.Exceptions
{
    public class AdjuntoInvalidoException : Exception
    {
        public AdjuntoInvalidoException(string message) : base(message) { }
    }
}