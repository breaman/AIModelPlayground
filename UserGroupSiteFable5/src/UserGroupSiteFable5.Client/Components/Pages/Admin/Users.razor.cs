using System.Security.Claims;

using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace UserGroupSiteFable5.Client.Components.Pages.Admin;

public partial class Users : ComponentBase
{
    [Inject] private IUserAdminService UserAdminService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [PersistentState]
    public List<UserAdminDto>? UserList { get; set; }

    private int _currentUserId;
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            _ = int.TryParse(authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out _currentUserId);
        }

        UserList ??= await UserAdminService.GetUsersAsync();
    }

    /// <summary>The current admin's own row gets a disabled Admin toggle (self-lockout guard).</summary>
    private bool IsSelf(UserAdminDto user)
    {
        return user.Id == _currentUserId;
    }

    private async Task OnAdminChangedAsync(UserAdminDto user, bool isAdmin)
    {
        await SaveRolesAsync(user, isAdmin, user.IsSpeaker);
    }

    private async Task OnSpeakerChangedAsync(UserAdminDto user, bool isSpeaker)
    {
        await SaveRolesAsync(user, user.IsAdmin, isSpeaker);
    }

    private async Task SaveRolesAsync(UserAdminDto user, bool isAdmin, bool isSpeaker)
    {
        _saving = true;
        try
        {
            var result = await UserAdminService.UpdateUserRolesAsync(user.Id,
                new UpdateUserRolesDto { IsAdmin = isAdmin, IsSpeaker = isSpeaker });

            if (result.Success)
            {
                user.IsAdmin = isAdmin;
                user.IsSpeaker = isSpeaker;
                ToastService.ShowSuccess($"Updated roles for {user.DisplayName}.");
            }
            else
            {
                ToastService.ShowError(result.Error ?? "Failed to update roles.");
            }
        }
        catch (Exception)
        {
            ToastService.ShowError("Something went wrong while saving. Please try again.");
        }
        finally
        {
            // Re-render so a rejected toggle snaps back to the saved state.
            _saving = false;
        }
    }
}
