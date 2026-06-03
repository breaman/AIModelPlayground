using System.Net.Http.Json;

using UserGroupSiteQwen35.Shared.Dto;

namespace UserGroupSiteQwen35.Client.Services;

public class UserManagementClientService : IUserManagementClientService
{
    private readonly HttpClient _httpClient;

    public UserManagementClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<UserWithRolesDto>> GetAllUsersAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<UserWithRolesDto>>("/api/users") ?? Enumerable.Empty<UserWithRolesDto>();
    }

    public async Task<UserWithRolesDto?> GetUserByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<UserWithRolesDto>($"/api/users/{id}");
    }

    public async Task<UserWithRolesDto?> GetCurrentUserAsync()
    {
        return await _httpClient.GetFromJsonAsync<UserWithRolesDto>("/api/users/current");
    }

    public async Task<bool> AddRoleAsync(int userId, string role)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/users/{userId}/role", new { role, operation = "add" });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveRoleAsync(int userId, string role)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/users/{userId}/role", new { role, operation = "remove" });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> IsUserInRoleAsync(int userId, string role)
    {
        return await _httpClient.GetFromJsonAsync<bool>($"/api/users/{userId}/role/{role}");
    }
}
