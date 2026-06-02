namespace UserGroupSiteMiniMaxM3.Shared.Models;

/// <summary>
/// Summary view of a user for the admin user-management page. Used by
/// both the server-side <c>UserAdminService</c> and the client-side
/// HTTP wrapper so the same DTO is shared end-to-end.
/// </summary>
/// <param name="Id">The user's primary key.</param>
/// <param name="UserName">Identity user name (typically the email).</param>
/// <param name="Email">Email address.</param>
/// <param name="FirstName">First name (nullable).</param>
/// <param name="LastName">Last name (nullable).</param>
/// <param name="MemberSince">When the user joined (UTC).</param>
/// <param name="Roles">Names of the roles the user currently holds.</param>
public record UserSummary(
    int Id,
    string UserName,
    string? Email,
    string? FirstName,
    string? LastName,
    DateTime MemberSince,
    IReadOnlyList<string> Roles);