using UserGroupSiteSonnet46.Shared.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace UserGroupSiteSonnet46.Client.Components.Pages.Admin;

/// <summary>Admin page for viewing all users and toggling Admin/Speaker roles.</summary>
public partial class UserList : ComponentBase
{
    [Inject] private IUserManagementService UserManagementService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [PersistentState]
    public List<UserDto>? Users { get; set; }

    private int _currentUserId;

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            var idClaim = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (int.TryParse(idClaim?.Value, out var id))
            {
                _currentUserId = id;
            }
        }

        Users ??= await UserManagementService.GetAllUsersAsync();
    }

    private bool IsCurrentUser(int userId) => userId == _currentUserId;

    private Task OnAdminRoleChanged(UserDto user, Microsoft.AspNetCore.Components.ChangeEventArgs e) =>
        OnRoleChanged(user, "Admin", e);

    private Task OnSpeakerRoleChanged(UserDto user, Microsoft.AspNetCore.Components.ChangeEventArgs e) =>
        OnRoleChanged(user, "Speaker", e);

    private async Task OnRoleChanged(UserDto user, string role, Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        var enabled = e.Value is bool b ? b : bool.Parse(e.Value?.ToString() ?? "false");

        try
        {
            await UserManagementService.SetRoleAsync(user.Id, role, enabled);
            ToastService.ShowSuccess($"{role} role {(enabled ? "granted to" : "removed from")} {user.FirstName} {user.LastName}.");
            Users = await UserManagementService.GetAllUsersAsync();
        }
        catch (Exception)
        {
            ToastService.ShowError($"Failed to update {role} role.");
        }
    }
}
