using System;
using SharedKernel.Exceptions;

namespace CatalogoArticulosBC.Domain.Exceptions
{
    public class MultimediaInvalidaException : DomainException
    {
        public MultimediaInvalidaException(string mensaje)
            : base(
                message: mensaje,
                code: "MultimediaInvalida",
                innerException: null)
        { }
    }
}