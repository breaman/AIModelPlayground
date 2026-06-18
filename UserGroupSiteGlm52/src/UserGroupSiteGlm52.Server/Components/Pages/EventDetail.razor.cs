using Ganss.Xss;

using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Server.Components.Pages;

/// <summary>
/// Public event detail (static SSR). Renders the Markdown description to HTML,
/// sanitizes it server-side, and emits it as a safe <see cref="MarkupString"/>.
/// Unpublished events are visible only to admins and assigned speakers.
/// </summary>
public partial class EventDetail : ComponentBase
{
    [Parameter]
    public string Slug { get; set; } = "";

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private IMarkdownService MarkdownService { get; set; } = default!;

    [Inject]
    private HtmlSanitizer Sanitizer { get; set; } = default!;

    public EventDetailDto? Event { get; set; }

    public MarkupString DescriptionHtml { get; set; }

    public bool NotFound { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Event = await EventService.GetPublishedBySlugAsync(Slug);
        if (Event is null)
        {
            // The service returns null for missing events and for unpublished
            // events the current user may not preview.
            NotFound = true;
            return;
        }

        // Render Markdown, then sanitize before emitting (public, untrusted content).
        var raw = MarkdownService.ToHtml(Event.Description);
        DescriptionHtml = new MarkupString(Sanitizer.Sanitize(raw));
    }
}