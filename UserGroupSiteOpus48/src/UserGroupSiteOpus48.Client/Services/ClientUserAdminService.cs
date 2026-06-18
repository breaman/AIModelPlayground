using System.Net;
using System.Net.Http.Json;

using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Client.Services;

/// <summary>
/// WebAssembly <see cref="IUserAdminService"/> that calls the server's <c>/api/admin/users</c> endpoints.
/// </summary>
public class ClientUserAdminService(HttpClient http) : IUserAdminService
{
    public async Task<UserAdminDto[]> GetUsersAsync() =>
        await http.GetFromJsonAsync<UserAdminDto[]>("api/admin/users") ?? [];

    public async Task<OperationResult> SetRoleAsync(SetRoleRequest request)
    {
        var response = await http.PostAsJsonAsync("api/admin/users/role", request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<OperationResult>()
                ?? OperationResult.Fail("Unexpected empty response.");
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return OperationResult.Fail("You are not authorized to manage users.");
        }

        return OperationResult.Fail("The server could not process the request.");
    }
}