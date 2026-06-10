using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

using Markdig;

namespace UserGroupSiteNemoTron3.Server.Components.Pages;

public partial class EventDetail : ComponentBase
{
    [Inject]
    public IEventService EventService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [Parameter]
    public string Slug { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [PersistentState]
    public EventDto? Event { get; set; }

    public bool CanEdit { get; set; }
    public string RenderedDescription { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        Event ??= await EventService.GetEventBySlugAsync(Slug);

        if (Event != null && !string.IsNullOrWhiteSpace(Event.Description))
        {
            // Render markdown on server side using Markdig
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            RenderedDescription = Markdown.ToHtml(Event.Description, pipeline);
        }

        if (Event != null && AuthenticationState != null)
        {
            var authState = await AuthenticationState;
            var user = authState.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    CanEdit = await EventService.CanUserEditEventAsync(Event.Id, userId);
                }
            }
        }
    }
}