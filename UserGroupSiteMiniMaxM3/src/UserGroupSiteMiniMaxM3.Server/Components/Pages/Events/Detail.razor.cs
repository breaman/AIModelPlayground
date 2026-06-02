using System.Security.Claims;

using Microsoft.AspNetCore.Components;

using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Server.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;

namespace UserGroupSiteMiniMaxM3.Server.Components.Pages.Events;

public partial class Detail : ComponentBase
{
    [Parameter] public string Slug { get; set; } = string.Empty;

    [Inject] private IEventService EventService { get; set; } = default!;

    [CascadingParameter]
    private Task<System.Security.Claims.ClaimsPrincipal>? AuthTask { get; set; }

    private EventDto? _event;
    private string _renderedDescription = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        var user = AuthTask is not null ? await AuthTask : new ClaimsPrincipal();
        _event = await EventService.GetBySlugAsync(Slug, user);
        if (_event is not null)
        {
            _renderedDescription = MarkdownRenderer.Render(_event.Description);
        }
    }
}