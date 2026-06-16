using System.Net.Http.Json;

using UserGroupSiteGpt55.Shared.Users;

namespace UserGroupSiteGpt55.Client.Services.Users;

/// <summary>
/// Implements admin user operations for WebAssembly by calling server APIs.
/// </summary>
public sealed class ClientUserAdminService(HttpClient httpClient) : IUserAdminService
{
    public async Task<IReadOnlyList<UserAdminListItem>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<UserAdminListItem>>("api/admin/users", cancellationToken) ?? [];
    }

    public async Task<UserRoleUpdateResult> UpdateRolesAsync(UserRoleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/admin/users/roles", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserRoleUpdateResult>(cancellationToken) ??
               UserRoleUpdateResult.Failure("The server returned an empty response.");
    }
}
