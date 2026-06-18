using System.Security.Claims;

namespace UserGroupSiteGpt55.Server.Services;

/// <summary>
/// Provides helpers for reading application-specific values from the current principal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the current integer user id when the principal is authenticated.
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}