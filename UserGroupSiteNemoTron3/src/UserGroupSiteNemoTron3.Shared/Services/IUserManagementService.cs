using UserGroupSiteNemoTron3.Shared.DTOs;

namespace UserGroupSiteNemoTron3.Shared.Services;

public interface IUserManagementService
{
    Task<UserDto[]> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto, int currentUserId);
    Task<bool> IsCurrentUserAdminAsync(int currentUserId);
}