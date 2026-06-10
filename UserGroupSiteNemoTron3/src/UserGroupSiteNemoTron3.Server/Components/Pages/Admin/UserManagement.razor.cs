using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Server.Components.Pages.Admin;

public partial class UserManagement : ComponentBase
{
    [Inject]
    public IUserManagementService UserManagementService { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [PersistentState]
    public UserDto[]? Users { get; set; }

    public int CurrentUserId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Get current user ID
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            CurrentUserId = userId;
        }

        Users ??= await UserManagementService.GetAllUsersAsync();
    }

    private async Task UpdateUserRole(int userId, bool isAdmin, bool isSpeaker)
    {
        try
        {
            var dto = new UpdateUserRolesDto { IsAdmin = isAdmin, IsSpeaker = isSpeaker };
            var updatedUser = await UserManagementService.UpdateUserRolesAsync(userId, dto, CurrentUserId);

            // Update local state
            if (Users != null)
            {
                var index = Array.FindIndex(Users, u => u.Id == userId);
                if (index >= 0)
                {
                    var userArray = Users.ToArray();
                    userArray[index] = updatedUser;
                    Users = userArray;
                }
            }

            ToastService.ShowSuccess("User roles updated successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error updating roles: {ex.Message}");
            // Revert local state by reloading
            Users = await UserManagementService.GetAllUsersAsync();
        }
    }
}