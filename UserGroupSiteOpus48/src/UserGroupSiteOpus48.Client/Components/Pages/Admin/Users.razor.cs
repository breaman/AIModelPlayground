using System.Security.Claims;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteOpus48.Shared.Authorization;
using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Client.Components.Pages.Admin;

/// <summary>
/// Admin-only page to grant/revoke the Admin and Speaker roles. The current admin's own Admin
/// toggle is disabled in the UI; the server also rejects self-demotion as a safety net.
/// </summary>
public partial class Users : ComponentBase
{
    [Inject] private IUserAdminService UserAdminService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    /// <summary>The user list; populated during pre-render and restored on the client.</summary>
    [PersistentState]
    public UserAdminDto[]? UserList { get; set; }

    private int _currentUserId;

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            var idClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idClaim, out _currentUserId);
        }

        UserList ??= await UserAdminService.GetUsersAsync();
    }

    /// <summary>
    /// Handles a role checkbox toggle: calls the service, updates local state on success, and
    /// shows a toast either way. On failure the list is left unchanged and re-rendered so the
    /// checkbox reverts to its previous value.
    /// </summary>
    private async Task OnRoleChangedAsync(UserAdminDto user, string role, bool inRole)
    {
        var result = await UserAdminService.SetRoleAsync(new SetRoleRequest(user.Id, role, inRole));

        if (result.Success)
        {
            UpdateLocalUser(user, role, inRole);
            ToastService.ShowSuccess($"{DisplayName(user)} {(inRole ? "granted" : "removed from")} {role}.");
        }
        else
        {
            ToastService.ShowError(string.Join(" ", result.Errors));
        }

        // Re-render so the checkbox reflects the authoritative local state (revert on failure).
        StateHasChanged();
    }

    /// <summary>Replaces the affected user's record with updated role flags (records are immutable).</summary>
    private void UpdateLocalUser(UserAdminDto user, string role, bool inRole)
    {
        if (UserList is null)
        {
            return;
        }

        var updated = role == RoleNames.Admin
            ? user with { IsAdmin = inRole }
            : user with { IsSpeaker = inRole };

        UserList = UserList.Select(u => u.Id == user.Id ? updated : u).ToArray();
    }

    private static string DisplayName(UserAdminDto user) =>
        string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
            ? user.Email ?? $"User {user.Id}"
            : $"{user.FirstName} {user.LastName}".Trim();
}