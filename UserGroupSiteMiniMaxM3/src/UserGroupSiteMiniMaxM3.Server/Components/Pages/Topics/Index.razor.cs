using System.Security.Claims;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Topics;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Server.Components.Pages.Topics;

/// <summary>
/// Topic suggestions page. Auth-gated. Lists all topics, lets the
/// current user suggest / vote / volunteer / delete (their own).
/// </summary>
public partial class Index : ComponentBase
{
    [Inject] private ITopicService TopicService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    private IReadOnlyList<TopicSummaryDto>? _topics;
    private bool _createOpen;
    private string _newTitle = string.Empty;
    private string? _newDescription;
    private Dictionary<string, string[]> _createErrors = new();
    private bool _busy;
    private bool _isAdmin;

    /// <summary>The current user's int id (used to check "is this mine").</summary>
    private int _currentUserDbId;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthState.GetAuthenticationStateAsync();
        _isAdmin = auth.User.IsInRole(Roles.Admin);

        var nameId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _currentUserDbId = int.TryParse(nameId, out var id) ? id : 0;

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var nameId = (await AuthState.GetAuthenticationStateAsync())
            .User.FindFirstValue(ClaimTypes.NameIdentifier);
        _topics = await TopicService.ListAsync(nameId);
    }

    private void ShowCreateForm()
    {
        _createOpen = true;
        _newTitle = string.Empty;
        _newDescription = null;
        _createErrors = new();
    }

    private void HideCreateForm() => _createOpen = false;

    private bool CanDelete(TopicSummaryDto t) => _isAdmin || t.SuggestedByUserId == _currentUserDbId;

    private async Task SubmitNew()
    {
        _busy = true;
        _createErrors = new();
        try
        {
            var nameId = (await AuthState.GetAuthenticationStateAsync())
                .User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await TopicService.CreateAsync(new TopicCreateDto
            {
                Title = _newTitle,
                Description = _newDescription,
            }, nameId);
            _createOpen = false;
            await ReloadAsync();
        }
        catch (TopicValidationException ex)
        {
            _createErrors = ex.Errors;
        }
        catch (Exception ex)
        {
            _createErrors = new() { ["_"] = new[] { ex.Message } };
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ToggleVoteAsync(int id)
    {
        _busy = true;
        try
        {
            var nameId = (await AuthState.GetAuthenticationStateAsync())
                .User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await TopicService.ToggleVoteAsync(id, nameId);
            await ReloadAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ToggleVolunteerAsync(int id)
    {
        _busy = true;
        try
        {
            var nameId = (await AuthState.GetAuthenticationStateAsync())
                .User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await TopicService.ToggleVolunteerAsync(id, nameId);
            await ReloadAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DeleteAsync(int id)
    {
        _busy = true;
        try
        {
            var nameId = (await AuthState.GetAuthenticationStateAsync())
                .User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await TopicService.DeleteAsync(id, nameId, _isAdmin);
            await ReloadAsync();
        }
        catch (UnauthorizedAccessException)
        {
            // Race: somebody else got admin and we lost it. Just reload.
            await ReloadAsync();
        }
        finally
        {
            _busy = false;
        }
    }
}