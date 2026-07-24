using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Client.Components.Pages;

public partial class AdminUsers : ComponentBase
{
    [Inject] private IUserAdministrationService UserService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [PersistentState]
    public IReadOnlyList<UserAdminDto>? Users { get; set; }

    protected int? BusyUserId { get; set; }
    protected bool LoadFailed { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            Users ??= await UserService.GetUsersAsync();
        }
        catch (HttpRequestException)
        {
            LoadFailed = true;
        }
    }

    /// <summary>Updates one independently toggled application role.</summary>
    protected async Task SetRoleAsync(UserAdminDto user, string role, bool enabled)
    {
        if (user.IsCurrentUser && role == AppRoles.Admin && !enabled)
        {
            ToastService.ShowWarning("You cannot remove your own Admin role.");
            return;
        }

        BusyUserId = user.Id;
        try
        {
            var request = new UpdateUserRolesRequest
            {
                IsAdmin = role == AppRoles.Admin ? enabled : IsInRole(user, AppRoles.Admin),
                IsSpeaker = role == AppRoles.Speaker ? enabled : IsInRole(user, AppRoles.Speaker)
            };
            var result = await UserService.UpdateRolesAsync(user.Id, request);
            if (result.Succeeded)
            {
                ToastService.ShowSuccess(result.Message ?? "Roles updated.");
                Users = await UserService.GetUsersAsync();
            }
            else
            {
                ToastService.ShowError(result.Message ?? "Roles could not be updated.");
            }
        }
        catch (HttpRequestException)
        {
            ToastService.ShowError("Roles could not be updated.");
            Users = await UserService.GetUsersAsync();
        }
        finally
        {
            BusyUserId = null;
        }
    }

    /// <summary>Checks role membership without relying on browser-supplied role values.</summary>
    protected static bool IsInRole(UserAdminDto user, string role) =>
        user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}