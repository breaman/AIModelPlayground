using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt55.Shared.Events;

namespace UserGroupSiteGpt55.Client.Components.Pages.Events;

public partial class EditableEvents : ComponentBase
{
    [Inject]
    private IEventService EventService { get; set; } = default!;

    [PersistentState]
    public IReadOnlyList<EventListItem>? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events ??= await EventService.GetEditableEventsAsync();
    }
}