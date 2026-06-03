using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteOpus48.Client.Components.Pages;

/// <summary>
/// Public home page listing published events, newest first. Visible to anonymous and authenticated
/// users. Uses the dual-mode <see cref="IEventService"/> with persisted state so the list pre-renders.
/// </summary>
public partial class Home : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    /// <summary>Published events; populated during pre-render and restored on the client.</summary>
    [PersistentState]
    public EventListItemDto[]? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events ??= await EventService.GetPublishedEventsAsync();
    }
}
