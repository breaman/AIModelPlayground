using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteOpus5.Server.Components.Pages;

/// <summary>
/// A single meeting's public page.
/// </summary>
/// <remarks>
/// Static server rendering, for the same reasons as the listing. The Markdown description is
/// rendered here rather than stored as HTML so the source stays editable and the sanitisation
/// decision lives in one place.
/// </remarks>
public partial class EventDetailPage : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    /// <summary>The event's URL slug, from the route.</summary>
    [Parameter]
    public string Slug { get; set; } = "";

    /// <summary>The meeting, or null when there is nothing the viewer may see at this slug.</summary>
    private EventDetail? Meeting { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // Drafts come back as null for anyone who is not an editor, so an unpublished event is
        // indistinguishable from a missing one to an ordinary visitor.
        Meeting = await EventService.GetEventBySlugAsync(Slug);
    }
}
