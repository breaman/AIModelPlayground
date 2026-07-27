using System.Net;

using UserGroupSiteOpus5.Tests.Infrastructure;

namespace UserGroupSiteOpus5.Tests;

/// <summary>
/// Covers how the real Identity cookie handler rejects unauthenticated API calls.
/// </summary>
/// <remarks>
/// Runs against the genuine cookie scheme rather than the test handler: the cookie handler's stock
/// challenge is a 302 to the login page, and a JSON caller receiving an HTML login page instead of
/// a status code cannot tell what went wrong.
/// </remarks>
[Collection(IntegrationTestCollection.Name)]
public class ApiChallengeTests : IAsyncLifetime
{
    private readonly ApiTestFactory _factory = new() { UseHeaderAuthentication = false };

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Theory]
    [InlineData("/api/admin/users")]
    [InlineData("/api/events/manage")]
    [InlineData("/api/speakers")]
    [InlineData("/api/topics")]
    public async Task AnUnauthenticatedApiCallGets401RatherThanALoginRedirect(string route)
    {
        using var client = CreateNonRedirectingClient();

        var response = await client.GetAsync(route);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnUnauthenticatedPageRequestIsSentToTheLoginPage()
    {
        // The status-code behaviour is scoped to /api; browser navigation still goes to login.
        using var client = CreateNonRedirectingClient();

        var response = await client.GetAsync("/admin/users");

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        var destination = response.StatusCode == HttpStatusCode.Redirect
            ? response.Headers.Location?.OriginalString ?? ""
            : await response.Content.ReadAsStringAsync();
        destination.ShouldContain("Account/Login");
    }

    private HttpClient CreateNonRedirectingClient()
    {
        // Following redirects would turn the response under test into the login page's 200.
        return _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
