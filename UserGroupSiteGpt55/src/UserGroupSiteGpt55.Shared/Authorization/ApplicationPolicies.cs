namespace UserGroupSiteGpt55.Shared.Authorization;

/// <summary>
/// Provides shared authorization policy names used by the server and Blazor UI.
/// </summary>
public static class ApplicationPolicies
{
    public const string AdminUsers = nameof(AdminUsers);
    public const string CreateEvents = nameof(CreateEvents);
    public const string EditEvents = nameof(EditEvents);
    public const string ManageTopics = nameof(ManageTopics);
}