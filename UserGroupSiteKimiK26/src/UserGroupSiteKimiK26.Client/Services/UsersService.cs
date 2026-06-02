using System.Net.Http.Json;

using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

namespace UserGroupSiteKimiK26.Client.Services;

public class UsersService(HttpClient httpClient) : IUsersService
{
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var response = await httpClient.GetAsync("api/users");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? [];
    }

    public async Task UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"api/users/{userId}/roles", dto);
        response.EnsureSuccessStatusCode();
    }
}
