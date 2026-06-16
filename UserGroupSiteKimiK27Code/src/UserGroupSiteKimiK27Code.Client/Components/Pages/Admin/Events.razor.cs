using System.Security.Claims;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Client.Components.Pages.Admin;

public partial class Events : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private List<EventListItemDto>? _events;
    private int _currentUserId;

    private bool IsMyEvents => NavigationManager.Uri.Contains("/admin/my-events", StringComparison.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = int.TryParse(authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        _events = await EventService.GetEditableEventsAsync();
        if (IsMyEvents)
        {
            // Filtering is already done server-side for speakers; keep this for robustness.
            _events = _events?.Where(e => e.Speakers.Any(s => s.Id == _currentUserId)).ToList();
        }
    }
}