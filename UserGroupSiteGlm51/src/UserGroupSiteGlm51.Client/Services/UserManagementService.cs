using System.Net.Http.Json;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Models;

namespace UserGroupSiteGlm51.Client.Services;

/// <summary>
/// Client-side implementation of <see cref="IUserManagementService"/> that calls
/// the server's minimal API endpoints via HttpClient.
/// </summary>
public class UserManagementService(HttpClient http) : IUserManagementService
{
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        var dtos = await http.GetFromJsonAsync<IEnumerable<UserDto>>("api/users") ?? [];
        return dtos.Select(MapFromDto);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        var dto = await http.GetFromJsonAsync<UserDto>($"api/users/{userId}");
        return dto is null ? null : MapFromDto(dto);
    }

    public async Task<IList<string>> GetUserRolesAsync(int userId)
    {
        var result = await http.GetFromJsonAsync<UserRoleDto>($"api/users/{userId}/roles");
        return result?.Roles ?? [];
    }

    public async Task AddToRoleAsync(int userId, string role)
    {
        var response = await http.PostAsync($"api/users/{userId}/roles/{role}", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveFromRoleAsync(int userId, string role)
    {
        var response = await http.DeleteAsync($"api/users/{userId}/roles/{role}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsLastAdminAsync(int userId)
    {
        // This check is handled server-side; client shouldn't need to call it directly
        // but implementing for interface compliance
        return await http.GetFromJsonAsync<bool>($"api/users/{userId}/is-last-admin");
    }

    private static User MapFromDto(UserDto dto) => new()
    {
        Id = dto.Id,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email
    };
}