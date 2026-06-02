namespace UserGroupSiteMiniMaxM3.Data.Models;

/// <summary>
/// Identity role names used throughout the application.
/// Centralized here to avoid magic strings scattered across the codebase
/// and to make role changes a single-file edit.
/// </summary>
public static class Roles
{
    /// <summary>Site administrator. Can manage users, all events, and topic suggestions.</summary>
    public const string Admin = "Admin";

    /// <summary>Designates a user as a speaker. A user can be a Speaker without being an Admin
    /// (the <see cref="Admin"/> and Speaker roles are independent; speaker assignment to an event
    /// is a relationship managed by the event's editor).</summary>
    public const string Speaker = "Speaker";
}