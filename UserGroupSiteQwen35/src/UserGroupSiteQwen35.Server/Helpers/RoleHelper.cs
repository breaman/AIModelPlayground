using System.Security.Claims;

using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Server.Helpers;

public static class RoleHelper
{
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole(RoleNames.Admin);
    }

    public static bool IsSpeaker(this ClaimsPrincipal user)
    {
        return user.IsInRole(RoleNames.Speaker);
    }

    public static bool IsAdminOrSpeaker(this ClaimsPrincipal user)
    {
        return user.IsInRole(RoleNames.Admin) || user.IsInRole(RoleNames.Speaker);
    }

    public static bool CanEditEvents(this ClaimsPrincipal user)
    {
        return user.IsAdminOrSpeaker();
    }
}