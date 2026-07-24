using System.Net.Http.Json;

using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Client.Services;

/// <summary>Calls the administrator user API from WebAssembly.</summary>
public sealed class ClientUserAdministrationService(
    HttpClient httpClient,
    AntiforgeryHttpClient writeClient) : IUserAdministrationService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAdminDto>> GetUsersAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<UserAdminDto>>("api/admin/users", cancellationToken) ?? [];

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateRolesAsync(int userId, UpdateUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await writeClient.SendAsync(HttpMethod.Put, $"api/admin/users/{userId}/roles", request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken) ??
               ServiceResult.Failure("The server returned an empty response.");
    }
}