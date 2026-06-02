using System.Net.Http.Json;

using UserGroupSiteMiniMaxM3.Shared.Models;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Client.Services;

/// <summary>
/// Client-side <see cref="IUserAdminService"/> that calls the server's
/// <c>/api/admin/users</c> endpoints over HTTP. Used by the WASM-rendered
/// admin user-management page.
/// </summary>
public class ClientUserAdminService(HttpClient http) : IUserAdminService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSummary>> ListUsersAsync(CancellationToken cancellationToken = default)
        => await http.GetFromJsonAsync<List<UserSummary>>("api/admin/users", cancellationToken) ?? [];

    /// <inheritdoc />
    public async Task UpdateRolesAsync(int targetUserId, bool isAdmin, bool isSpeaker, CancellationToken cancellationToken = default)
    {
        var response = await http.PutAsJsonAsync(
            $"api/admin/users/{targetUserId}/roles",
            new { IsAdmin = isAdmin, IsSpeaker = isSpeaker },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}