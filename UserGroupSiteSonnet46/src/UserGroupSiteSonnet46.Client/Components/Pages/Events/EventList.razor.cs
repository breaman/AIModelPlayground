using Microsoft.AspNetCore.Components;

using UserGroupSiteSonnet46.Shared.Services;

namespace UserGroupSiteSonnet46.Client.Components.Pages.Events;

/// <summary>Dashboard listing all events (published and draft) for authenticated users.</summary>
public partial class EventList : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    [PersistentState]
    public List<EventDto>? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events ??= await EventService.GetAllEventsAsync();
    }
}