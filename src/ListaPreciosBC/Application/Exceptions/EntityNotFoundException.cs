namespace ListaPreciosBC.Application
{
    using System;

    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string message) : base(message) { }
    }
}