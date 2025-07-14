namespace GestionClientesBC.Application.Interfaces
{
    public interface IUserContext
    {
        bool HasPermission(string permission);
    }
}