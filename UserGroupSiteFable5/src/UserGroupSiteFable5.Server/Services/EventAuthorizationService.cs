using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using UserGroupSiteFable5.Data.Models;

namespace UserGroupSiteFable5.Server.Services;

/// <inheritdoc cref="IEventAuthorizationService"/>
public class EventAuthorizationService(ApplicationDbContext dbContext) : IEventAuthorizationService
{
    public async Task<bool> CanEditEventAsync(ClaimsPrincipal user, int eventId)
    {
        if (user.IsInRole("Admin"))
        {
            return true;
        }

        if (!user.IsInRole("Speaker"))
        {
            return false;
        }

        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return false;
        }

        return await dbContext.EventSpeakers
            .AnyAsync(es => es.EventId == eventId && es.UserId == userId);
    }
}