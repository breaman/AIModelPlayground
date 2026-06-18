using Microsoft.AspNetCore.Components;

using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Client.Components.Pages.Events;

/// <summary>
/// Public event detail page, looked up by slug. Renders the stored Markdown description (server-rendered
/// to HTML), date/time, location, and speakers. Only published events are returned by the service.
/// </summary>
public partial class EventDetail : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    /// <summary>The event slug from the route.</summary>
    [Parameter] public string Slug { get; set; } = string.Empty;

    /// <summary>The loaded event; persisted across pre-render/hydration.</summary>
    [PersistentState]
    public EventDto? Event { get; set; }

    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        Event ??= await EventService.GetEventBySlugAsync(Slug);
        _loaded = true;
    }
}