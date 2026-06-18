using UserGroupSiteKimiK26.Shared.Dtos;

namespace UserGroupSiteKimiK26.Shared.Services;

public interface IUsersService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto);
}