using UserGroupSiteOpus5.Shared.Common;

namespace UserGroupSiteOpus5.Server.Endpoints;

/// <summary>
/// Rejects state-changing API calls that do not carry the client's custom request header.
/// </summary>
/// <remarks>
/// <para>
/// These endpoints authenticate with the Identity cookie, and <c>UseAntiforgery</c> does not
/// validate JSON request bodies, so without this a cross-site HTML form could post to them and the
/// browser would attach the victim's cookie.
/// </para>
/// <para>
/// A cross-site form cannot set a custom header, and a cross-origin <c>fetch</c> that sets one
/// triggers a CORS preflight that no policy here answers. Combined with the Identity cookie's
/// default <c>SameSite=Lax</c>, requiring the header closes the gap. Applied once to the mutating
/// route groups rather than repeated per endpoint.
/// </para>
/// </remarks>
public class RequireClientHeaderFilter : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var header = context.HttpContext.Request.Headers[ApiHeaders.RequestedWith].ToString();

        if (!string.Equals(header, ApiHeaders.RequestedWithValue, StringComparison.Ordinal))
        {
            return Results.Problem(
                title: "Missing client header.",
                detail: $"State-changing requests must include the {ApiHeaders.RequestedWith} header.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
