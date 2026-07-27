namespace UserGroupSiteOpus5.Shared.Common;

/// <summary>
/// Custom HTTP headers shared by the WebAssembly client and the API endpoints.
/// </summary>
public static class ApiHeaders
{
    /// <summary>
    /// Header the client attaches to every state-changing API call.
    /// </summary>
    /// <remarks>
    /// The JSON API authenticates with the Identity cookie, and <c>UseAntiforgery</c> does not
    /// validate JSON requests, so a cross-site form post could otherwise ride the cookie. A
    /// cross-site HTML form cannot set a custom request header, and a cross-origin
    /// <c>fetch</c> that tries to would be blocked by the preflight (no CORS policy is
    /// configured). Requiring this header therefore rejects the forged request while costing the
    /// real client one line in a <c>DelegatingHandler</c>.
    /// </remarks>
    public const string RequestedWith = "X-Requested-With";

    /// <summary>The value <see cref="RequestedWith"/> must carry.</summary>
    public const string RequestedWithValue = "UserGroupSiteClient";
}
