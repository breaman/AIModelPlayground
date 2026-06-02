using System.Net.Http.Json;

using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Client.Services;

public class ClientUserManagementService(HttpClient http) : IUserManagementService
{
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await http.GetFromJsonAsync<List<UserDto>>("api/users") ?? [];
    }

    public async Task UpdateUserRolesAsync(int userId, List<string> roles)
    {
        var response = await http.PutAsJsonAsync($"api/users/{userId}/roles", roles);
        response.EnsureSuccessStatusCode();
    }
}
