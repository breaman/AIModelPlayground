using System.Security.Claims;

namespace UserGroupSiteKimiK27Code.Shared.Services;

public interface IEventAuthorizationService
{
    bool IsAdmin(ClaimsPrincipal user);
    bool IsSpeaker(ClaimsPrincipal user);
    Task<bool> CanEditEventAsync(ClaimsPrincipal user, int eventId, CancellationToken cancellationToken = default);
}