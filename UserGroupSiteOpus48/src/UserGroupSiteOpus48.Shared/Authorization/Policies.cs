namespace UserGroupSiteOpus48.Shared.Authorization;

/// <summary>
/// Named authorization policy constants used by both the server (policy registration
/// and endpoint guards) and the client (route-level <c>[Authorize]</c> attributes).
/// </summary>
public static class Policies
{
    /// <summary>Requires the current user to be in the <see cref="RoleNames.Admin"/> role.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Requires the current user to be in the <see cref="RoleNames.Admin"/> or
    /// <see cref="RoleNames.Speaker"/> role. Gates event-editing entry points; per-event edit
    /// permission (assigned speaker) is checked additionally inside the service.
    /// </summary>
    public const string EventEditors = "EventEditors";
}
