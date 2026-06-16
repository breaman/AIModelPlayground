using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK27Code.Data.Models;
using UserGroupSiteKimiK27Code.Shared;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Server.Services;

public class EventAuthorizationService : IEventAuthorizationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public EventAuthorizationService(ApplicationDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(Roles.Admin);

    public bool IsSpeaker(ClaimsPrincipal user) => user.IsInRole(Roles.Speaker);

    public async Task<bool> CanEditEventAsync(ClaimsPrincipal user, int eventId, CancellationToken cancellationToken = default)
    {
        if (IsAdmin(user)) return true;

        var userId = _userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id)) return false;

        if (!IsSpeaker(user)) return false;

        return await _dbContext.EventSpeakers
            .AsNoTracking()
            .AnyAsync(es => es.EventId == eventId && es.UserId == id, cancellationToken);
    }
}