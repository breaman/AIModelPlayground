using System.Security.Claims;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteDeepSeekV4Pro.Shared.Constants;
using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Client.Components.Pages.Admin;

public partial class UserManagement : ComponentBase
{
    [Inject] private IUserManagementService UserService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    protected List<UserDto>? _users;
    protected int _currentUserId;

    protected override async Task OnInitializedAsync()
    {
        _users ??= await UserService.GetAllUsersAsync();

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _ = int.TryParse(userIdClaim, out _currentUserId);
    }

    private void ToggleRole(UserDto user, string role, bool isChecked)
    {
        if (isChecked && !user.Roles.Contains(role))
        {
            user.Roles.Add(role);
        }
        else if (!isChecked && user.Roles.Contains(role))
        {
            user.Roles.Remove(role);
        }
    }

    private async Task SaveRoles(UserDto user)
    {
        try
        {
            await UserService.UpdateUserRolesAsync(user.Id, user.Roles);
            ToastService.ShowSuccess($"Roles updated for {user.FullName}.");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to update roles: {ex.Message}");
            // Refresh to revert UI
            _users = await UserService.GetAllUsersAsync();
        }
    }
}
