using Microsoft.AspNetCore.Authorization;

namespace UserGroupSiteOpus5.Server.Authorization;

/// <summary>
/// Requires that the user may edit a particular event.
/// </summary>
/// <remarks>
/// This is deliberately resource-based rather than role-based. A blanket
/// <c>[Authorize(Roles = "Speaker")]</c> would let any speaker edit any event, which is not the
/// rule: a speaker may edit only the events they are assigned to.
/// </remarks>
public class EventEditRequirement : IAuthorizationRequirement;
