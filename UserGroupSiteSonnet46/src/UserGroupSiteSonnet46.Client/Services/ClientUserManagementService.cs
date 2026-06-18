using System.Net.Http.Json;

using UserGroupSiteSonnet46.Shared.Services;

namespace UserGroupSiteSonnet46.Client.Services;

/// <summary>
/// WebAssembly-side implementation that delegates to the /api/users HTTP API.
/// </summary>
public class ClientUserManagementService(HttpClient http) : IUserManagementService
{
    /// <inheritdoc />
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await http.GetFromJsonAsync<List<UserDto>>("api/users") ?? [];
    }

    /// <inheritdoc />
    public async Task SetRoleAsync(int userId, string role, bool enabled)
    {
        var response = await http.PostAsJsonAsync($"api/users/{userId}/roles", new { role, enabled });
        response.EnsureSuccessStatusCode();
    }
}