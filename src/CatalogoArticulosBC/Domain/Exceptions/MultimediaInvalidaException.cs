using System;

namespace CatalogoArticulosBC.Domain.Exceptions
{
    public class MultimediaInvalidaException : Exception
    {
        public MultimediaInvalidaException(string mensaje) : base(mensaje) { }
    }
}