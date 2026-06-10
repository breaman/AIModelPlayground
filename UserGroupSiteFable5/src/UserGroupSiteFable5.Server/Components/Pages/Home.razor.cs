using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Shared.Dtos;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteFable5.Server.Components.Pages;

/// <summary>
/// Public home page (static SSR, anonymous): lists published events newest-first,
/// reading the database directly — no API hop needed on the server.
/// </summary>
public partial class Home : ComponentBase
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;

    private List<EventDetailDto>? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events = await DbContext.Events
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.StartsAt)
            .Select(e => new EventDetailDto
            {
                Id = e.Id,
                Title = e.Title,
                Slug = e.Slug,
                ShortDescription = e.ShortDescription,
                Description = e.Description,
                StartsAt = e.StartsAt,
                Location = e.Location,
                // Inline expression (not a helper method) so EF can translate it to SQL
                // inside this nested collection projection.
                SpeakerNames = e.Speakers
                    .Select(s => s.User.FirstName == null && s.User.LastName == null
                        ? s.User.Email ?? ""
                        : ((s.User.FirstName ?? "") + " " + (s.User.LastName ?? "")).Trim())
                    .ToList()
            })
            .ToListAsync();
    }
}
