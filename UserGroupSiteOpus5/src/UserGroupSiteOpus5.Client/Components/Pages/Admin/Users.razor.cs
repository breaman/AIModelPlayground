using System.Security.Claims;

using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace UserGroupSiteOpus5.Client.Components.Pages.Admin;

/// <summary>
/// Member and role administration.
/// </summary>
/// <remarks>
/// The signed-in administrator's own Admin checkbox is rendered disabled. That is a courtesy, not
/// the rule: <c>ServerUserAdminService.UpdateUserRolesAsync</c> rejects the same change, so a
/// hand-crafted request to the API cannot demote its own author either.
/// </remarks>
public partial class Users : ComponentBase
{
    [Inject] private IUserAdminService UserAdminService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    /// <summary>Every member. Null while loading.</summary>
    [PersistentState]
    public IReadOnlyList<UserListItem>? AllUsers { get; set; }

    /// <summary>The signed-in administrator's user id, used for the self-demotion guard.</summary>
    private int? CurrentUserId { get; set; }

    /// <summary>The active search term.</summary>
    private string SearchTerm { get; set; } = "";

    /// <summary>User ids with a role change in flight.</summary>
    private readonly HashSet<int> _busyUserIds = [];

    /// <summary>The members matching <see cref="SearchTerm"/>.</summary>
    private IReadOnlyList<UserListItem> VisibleUsers =>
        AllUsers is null
            ? []
            : string.IsNullOrWhiteSpace(SearchTerm)
                ? AllUsers
                : AllUsers
                    .Where(x =>
                        x.DisplayName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (x.Email?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            // FindFirstValue is a server-side extension method, so read the claim directly.
            if (int.TryParse(authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id))
            {
                CurrentUserId = id;
            }
        }

        if (AllUsers is null)
        {
            await ReloadAsync();
        }
    }

    /// <summary>Whether a role change is in flight for the given member.</summary>
    private bool IsBusy(int userId)
    {
        return _busyUserIds.Contains(userId);
    }

    /// <summary>Records the search term as the administrator types.</summary>
    private void OnSearchChanged(ChangeEventArgs args)
    {
        SearchTerm = args.Value?.ToString() ?? "";
    }

    /// <summary>Applies a role change and reconciles the row from the server.</summary>
    private async Task SetRolesAsync(UserListItem member, bool isAdmin, bool isSpeaker)
    {
        if (AllUsers is null || IsBusy(member.Id))
        {
            return;
        }

        _busyUserIds.Add(member.Id);

        try
        {
            var result = await UserAdminService.UpdateUserRolesAsync(
                new UserRoleUpdateModel(member.Id, isAdmin, isSpeaker));

            if (!result.Succeeded)
            {
                ToastService.ShowError(string.Join(" ", result.Errors), "Roles unchanged");
                // Re-read from the server so the checkbox snaps back to the state that actually
                // holds rather than the one the click implied.
                await ReloadAsync();
                return;
            }

            AllUsers = AllUsers
                .Select(x => x.Id == member.Id ? x with { IsAdmin = isAdmin, IsSpeaker = isSpeaker } : x)
                .ToList();

            ToastService.ShowSuccess($"Updated roles for {member.DisplayName}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ToastService.ShowError("The role change did not go through. Try again.");
            await ReloadAsync();
        }
        finally
        {
            _busyUserIds.Remove(member.Id);
        }
    }

    /// <summary>Refetches the member list.</summary>
    private async Task ReloadAsync()
    {
        try
        {
            AllUsers = await UserAdminService.GetUsersAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            AllUsers ??= [];
            ToastService.ShowError("Could not load the member list.");
        }
    }
}
