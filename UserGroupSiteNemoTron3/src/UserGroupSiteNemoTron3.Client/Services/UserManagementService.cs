using System.Net.Http.Json;

using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Client.Services;

public class UserManagementService : IUserManagementService
{
    private readonly HttpClient _httpClient;

    public UserManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserDto[]> GetAllUsersAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<UserDto[]>("/api/admin/users");
        return result ?? [];
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<UserDto>($"/api/admin/users/{id}");
    }

    public async Task<UserDto> UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto, int currentUserId)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/admin/users/{userId}/roles", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    public async Task<bool> IsCurrentUserAdminAsync(int currentUserId)
    {
        // This would need a dedicated endpoint; for now, we'll assume the server handles authorization
        return false;
    }
}