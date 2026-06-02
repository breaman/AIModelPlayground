using System.Security.Claims;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;

namespace UserGroupSiteMiniMaxM3.Server.Components.Pages.Events;

public partial class Manage : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

    private IReadOnlyList<EventSummaryDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        var authState = AuthStateTask is not null ? await AuthStateTask : null;
        var user = authState?.User ?? new ClaimsPrincipal();
        _events = await EventService.ListForUserAsync(user);
    }
}