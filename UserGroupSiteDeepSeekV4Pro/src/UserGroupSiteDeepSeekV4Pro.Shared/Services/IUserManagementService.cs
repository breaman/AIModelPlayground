using UserGroupSiteDeepSeekV4Pro.Shared.Models;

namespace UserGroupSiteDeepSeekV4Pro.Shared.Services;

public interface IUserManagementService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task UpdateUserRolesAsync(int userId, List<string> roles);
}