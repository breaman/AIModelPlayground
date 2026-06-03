using UserGroupSiteSonnet46.Shared.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace UserGroupSiteSonnet46.Server.Components.Pages.Events;

/// <summary>
/// Publicly accessible SSR event detail page. Renders full Markdown description.
/// Admins and the event's assigned speakers see an Edit button.
/// </summary>
public partial class EventDetail : ComponentBase
{
    [Parameter] public string Slug { get; set; } = string.Empty;

    [Inject] private IEventService EventService { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    public EventDto? Event { get; set; }

    public bool CanEdit { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Event = await EventService.GetEventBySlugAsync(Slug);

        if (Event is null)
        {
            return;
        }

        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var isAdmin = user.IsInRole("Admin");
                var userIdStr = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdStr, out var userId);
                var isSpeaker = Event.Speakers.Any(s => s.Id == userId);

                CanEdit = isAdmin || isSpeaker;
            }
        }
    }
}
