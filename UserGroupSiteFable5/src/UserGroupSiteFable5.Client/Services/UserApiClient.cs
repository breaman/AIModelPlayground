using System.Net.Http.Json;

using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Client.Services;

/// <summary>WASM-side <see cref="IUserAdminService"/> that calls the server's admin user API.</summary>
public class UserApiClient(HttpClient http) : IUserAdminService
{
    public async Task<List<UserAdminDto>> GetUsersAsync()
    {
        return await http.GetFromJsonAsync<List<UserAdminDto>>("api/users") ?? [];
    }

    public async Task<ServiceResult> UpdateUserRolesAsync(int userId, UpdateUserRolesDto roles)
    {
        var response = await http.PutAsJsonAsync($"api/users/{userId}/roles", roles);

        return await HttpServiceResult.FromResponseAsync(response);
    }
}
