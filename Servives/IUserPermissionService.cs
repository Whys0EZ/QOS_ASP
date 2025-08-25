namespace QOS.Services
{
    public interface IUserPermissionService
    {
        bool HasPermission(string username, string functionCode);
    }
}