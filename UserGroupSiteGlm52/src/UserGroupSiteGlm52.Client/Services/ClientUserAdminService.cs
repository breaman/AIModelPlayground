using System.Net.Http.Json;

using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Client.Services;

/// <summary>HTTP-backed <see cref="IUserAdminService"/> for WebAssembly.</summary>
public sealed class ClientUserAdminService(HttpClient http) : IUserAdminService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserWithRolesDto>> GetUsersAsync()
    {
        return await http.GetFromJsonAsync<IReadOnlyList<UserWithRolesDto>>("api/admin/users") ?? [];
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateRolesAsync(int id, bool isAdmin, bool isSpeaker)
    {
        var response = await http.PutAsJsonAsync($"api/admin/users/{id}/roles",
            new { isAdmin, isSpeaker });
        return await ClientResults.ReadVoidAsync(response);
    }
}