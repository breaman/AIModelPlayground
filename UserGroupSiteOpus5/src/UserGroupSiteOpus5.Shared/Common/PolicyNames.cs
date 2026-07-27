namespace UserGroupSiteOpus5.Shared.Common;

/// <summary>
/// Authorization policy names shared by endpoint registration, page attributes, and tests.
/// </summary>
public static class PolicyNames
{
    /// <summary>Requires the <see cref="RoleNames.Admin"/> role.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Requires either the <see cref="RoleNames.Admin"/> or <see cref="RoleNames.Speaker"/> role.</summary>
    public const string SpeakerOrAdmin = "SpeakerOrAdmin";

    /// <summary>
    /// Resource-based policy satisfied by any admin, or by a speaker assigned to the event being
    /// edited. Evaluated against an event id; see the event editor authorization handler.
    /// </summary>
    public const string EventEditor = "EventEditor";
}
