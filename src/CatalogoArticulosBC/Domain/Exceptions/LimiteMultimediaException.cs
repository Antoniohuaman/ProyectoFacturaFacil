using System;
using SharedKernel.Exceptions;

namespace CatalogoArticulosBC.Domain.Exceptions
{
    public class LimiteMultimediaException : DomainException
    {
        public LimiteMultimediaException()
            : base(
                message: "El producto ya tiene el máximo de archivos permitidos (5).",
                code: "LimiteMultimedia",
                innerException: null)
        { }
    }
}