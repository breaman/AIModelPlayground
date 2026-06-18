using System.Security.Claims;

using UserGroupSiteGpt55.Data.Interfaces;

namespace UserGroupSiteGpt55.Server.Services;

public class HttpUserService : IUserService
{
    private IHttpContextAccessor HttpContextAccessor { get; }
    public HttpUserService(IHttpContextAccessor httpContextAccessor)
    {
        HttpContextAccessor = httpContextAccessor;
    }
    public int UserId
    {
        get
        {
            var value = HttpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var userId) ? userId : default;
        }
    }
}