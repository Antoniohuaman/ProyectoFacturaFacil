using System;

namespace CatalogoArticulosBC.Domain.Exceptions
{
    public class LimiteMultimediaException : Exception
    {
        public LimiteMultimediaException() : base("El producto ya tiene el máximo de archivos permitidos (5).") { }
    }
}