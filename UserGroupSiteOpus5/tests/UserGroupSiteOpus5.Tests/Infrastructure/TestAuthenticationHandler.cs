using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UserGroupSiteOpus5.Tests.Infrastructure;

/// <summary>
/// Authenticates integration-test requests from headers instead of an Identity cookie.
/// </summary>
/// <remarks>
/// Signing in through the real cookie pipeline would test Identity, which is framework code. What
/// these tests need to exercise is the endpoint layer: routing, the authorization policies, and
/// the anti-CSRF filter. Supplying the principal directly keeps the test focused on that.
/// </remarks>
public class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The scheme name this handler registers under.</summary>
    public const string SchemeName = "TestScheme";

    /// <summary>Header carrying the user id to authenticate as. Absent means anonymous.</summary>
    public const string UserIdHeader = "X-Test-UserId";

    /// <summary>Header carrying a comma-separated list of roles.</summary>
    public const string RolesHeader = "X-Test-Roles";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userId) || string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, $"user{userId}")
        };

        if (Request.Headers.TryGetValue(RolesHeader, out var roles))
        {
            claims.AddRange(roles.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
