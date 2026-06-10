using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace UserGroupSiteFable5.Client.Components.Pages.Events;

public partial class EventList : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [PersistentState]
    public List<EventSummaryDto>? Events { get; set; }

    [PersistentState]
    public bool IsAdmin { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            IsAdmin = authState.User.IsInRole("Admin");
        }

        Events ??= await EventService.GetEditableEventsAsync();
    }
}
