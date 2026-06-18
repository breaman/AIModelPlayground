using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Shared.Services;

namespace UserGroupSiteGlm51.Server.Components.Pages.Admin;

/// <summary>
/// Admin page for managing users and their role assignments.
/// </summary>
public partial class UserList : ComponentBase
{
    private const string AdminRole = "Admin";
    private const string SpeakerRole = "Speaker";

    [Inject]
    private IUserManagementService UserManagementService { get; set; } = default!;

    [Inject]
    private IUserService UserService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    /// <summary>
    /// Users with their role assignments for display.
    /// </summary>
    public List<UserWithRoles>? AllUsers { get; set; }

    /// <summary>
    /// The current logged-in user's ID, used to prevent self-removal from Admin.
    /// </summary>
    public int CurrentUserId => UserService.UserId;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
    }

    private async Task LoadUsers()
    {
        var users = await UserManagementService.GetAllUsersAsync();
        var userWithRoles = new List<UserWithRoles>();

        foreach (var user in users)
        {
            var roles = await UserManagementService.GetUserRolesAsync(user.Id);
            userWithRoles.Add(new UserWithRoles(user.Id, user.FirstName, user.LastName, user.Email, roles.ToList()));
        }

        AllUsers = userWithRoles;
    }

    private async Task AddToRole(int userId, string role)
    {
        try
        {
            await UserManagementService.AddToRoleAsync(userId, role);
            ToastService.ShowSuccess($"User added to {role} role.");
            await LoadUsers();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error adding role: {ex.Message}");
        }
    }

    private async Task RemoveFromRole(int userId, string role)
    {
        try
        {
            // Prevent removing yourself from Admin role
            if (role == "Admin" && userId == CurrentUserId)
            {
                ToastService.ShowError("You cannot remove yourself from the Admin role.");
                return;
            }

            // Check if this is the last admin
            if (role == "Admin" && await UserManagementService.IsLastAdminAsync(userId))
            {
                ToastService.ShowError("Cannot remove the last Admin user.");
                return;
            }

            await UserManagementService.RemoveFromRoleAsync(userId, role);
            ToastService.ShowSuccess($"User removed from {role} role.");
            await LoadUsers();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error removing role: {ex.Message}");
        }
    }
}

/// <summary>
/// View model combining user info with role assignments for display.
/// </summary>
public class UserWithRoles(int id, string? firstName, string? lastName, string? email, List<string> roles)
{
    public int Id { get; } = id;
    public string? FirstName { get; } = firstName;
    public string? LastName { get; } = lastName;
    public string? Email { get; } = email;
    public List<string> Roles { get; } = roles;
}