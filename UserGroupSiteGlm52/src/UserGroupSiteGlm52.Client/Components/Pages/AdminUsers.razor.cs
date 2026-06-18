using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Client.Components.Pages;

/// <summary>Admin user management: set the Admin and Speaker roles for each user.</summary>
public partial class AdminUsers : ComponentBase
{
    [Inject]
    private IUserAdminService UserAdminService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    [PersistentState]
    public IReadOnlyList<UserWithRolesDto>? Users { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Dual-mode: only fetch when state was not restored during hydration.
        Users ??= await UserAdminService.GetUsersAsync();
    }

    /// <summary>Persist the new role flags; re-fetches to stay in sync (and revert on failure).</summary>
    private async Task SetRolesAsync(UserWithRolesDto user, bool isAdmin, bool isSpeaker)
    {
        var result = await UserAdminService.UpdateRolesAsync(user.Id, isAdmin, isSpeaker);
        if (result.Succeeded)
        {
            ToastService.ShowSuccess($"Roles updated for {user.DisplayName}.", "Saved");
        }
        else
        {
            ShowErrors(result);
        }

        // Re-fetch so the UI reflects the server (reverts the checkbox if the server rejected).
        Users = await UserAdminService.GetUsersAsync();
    }

    private void ShowErrors(ServiceResult result)
    {
        var message = result.ValidationErrors.Count > 0
            ? string.Join(" ", result.ValidationErrors.SelectMany(kv => kv.Value))
            : (result.Errors.Count > 0 ? string.Join(" ", result.Errors) : "Something went wrong.");
        ToastService.ShowError(message, "Update failed");
    }
}