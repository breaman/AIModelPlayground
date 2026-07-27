using System.Net.Http.Json;

using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

namespace UserGroupSiteOpus5.Client.Services;

/// <summary>
/// WebAssembly implementation of <see cref="IUserAdminService"/>, calling the server's HTTP API.
/// </summary>
public class ClientUserAdminService(HttpClient http) : IUserAdminService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserListItem>> GetUsersAsync()
    {
        return await http.GetFromJsonAsync<List<UserListItem>>("api/admin/users") ?? [];
    }

    /// <inheritdoc />
    public async Task<SaveResult> UpdateUserRolesAsync(UserRoleUpdateModel model)
    {
        var response = await http.PutAsJsonAsync($"api/admin/users/{model.UserId}/roles", model);
        return await response.ReadSaveResultAsync();
    }
}
