using UserGroupSiteQwen35.Shared.Dto;

namespace UserGroupSiteQwen35.Client.Services;

public interface IUserManagementClientService
{
    Task<IEnumerable<UserWithRolesDto>> GetAllUsersAsync();
    Task<UserWithRolesDto?> GetUserByIdAsync(int id);
    Task<UserWithRolesDto?> GetCurrentUserAsync();
    Task<bool> AddRoleAsync(int userId, string role);
    Task<bool> RemoveRoleAsync(int userId, string role);
    Task<bool> IsUserInRoleAsync(int userId, string role);
}
