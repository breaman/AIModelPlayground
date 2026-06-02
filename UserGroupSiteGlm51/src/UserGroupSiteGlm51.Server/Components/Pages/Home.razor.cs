using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteGlm51.Server.Components.Pages;

/// <summary>
/// Home page displaying published events to all visitors.
/// </summary>
public partial class Home : ComponentBase
{
    [Inject]
    private IEventService EventService { get; set; } = default!;

    /// <summary>
    /// List of published events displayed on the home page.
    /// </summary>
    public IEnumerable<Event>? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events = await EventService.GetPublishedEventsAsync();
    }
}