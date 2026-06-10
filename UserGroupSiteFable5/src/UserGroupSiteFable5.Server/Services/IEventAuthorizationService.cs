using System.Security.Claims;

namespace UserGroupSiteFable5.Server.Services;

/// <summary>
/// Resource-based authorization for event editing: an editor is an Admin or a speaker
/// assigned to that specific event. Kept as a service (rather than a static policy)
/// so the per-event check is testable and reusable across endpoints.
/// </summary>
public interface IEventAuthorizationService
{
    /// <summary>Whether <paramref name="user"/> may edit the event with the given id.</summary>
    Task<bool> CanEditEventAsync(ClaimsPrincipal user, int eventId);
}
