using System.Security.Claims;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteMiniMaxM3.Shared.Constants;
using UserGroupSiteMiniMaxM3.Shared.Models;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Client.Components.Pages.Admin;

public partial class Users : ComponentBase
{
    [Inject] private IUserAdminService UserAdminService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    private IReadOnlyList<UserSummary>? _users;
    private UserSummary? _editing;
    private bool _editIsAdmin;
    private bool _editIsSpeaker;
    private string? _editError;
    private bool _saving;
    private string? _loadError;
    private int _currentUserId;

    /// <summary>
    /// Pre-rendered list of users. Populated server-side during the
    /// interactive WebAssembly pre-render pass so the page has data on
    /// first paint; refetched after hydration to keep it fresh.
    /// </summary>
    [PersistentState]
    public IReadOnlyList<UserSummary>? InitialUsers { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _currentUserId = int.TryParse(userIdClaim, out var id) ? id : 0;

        if (InitialUsers is not null)
        {
            _users = InitialUsers;
        }
        else
        {
            await LoadUsersAsync();
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            _users = await UserAdminService.ListUsersAsync();
        }
        catch (Exception ex)
        {
            _loadError = $"Failed to load users: {ex.Message}";
        }
    }

    private void BeginEdit(UserSummary u)
    {
        _editing = u;
        _editIsAdmin = u.Roles.Contains(Roles.Admin);
        _editIsSpeaker = u.Roles.Contains(Roles.Speaker);
        _editError = null;
    }

    private void CancelEdit()
    {
        _editing = null;
        _editError = null;
    }

    private async Task SaveEdit()
    {
        if (_editing is null) return;
        _saving = true;
        _editError = null;
        try
        {
            await UserAdminService.UpdateRolesAsync(_editing.Id, _editIsAdmin, _editIsSpeaker);
            _editing = null;
            ToastService.ShowSuccess("Roles updated");
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _editError = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }
}