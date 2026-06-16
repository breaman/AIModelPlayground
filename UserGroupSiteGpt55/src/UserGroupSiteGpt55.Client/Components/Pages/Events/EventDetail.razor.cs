using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt55.Shared.Events;

namespace UserGroupSiteGpt55.Client.Components.Pages.Events;

public partial class EventDetail : ComponentBase
{
    [Parameter]
    public string Slug { get; set; } = "";

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [PersistentState]
    public UserGroupSiteGpt55.Shared.Events.EventDetail? EventModel { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        EventModel ??= await EventService.GetEventBySlugAsync(Slug);
    }
}
