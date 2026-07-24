using System.Net.Http.Json;

namespace UserGroupSiteGpt56Sol.Client.Services;

/// <summary>Adds a fresh ASP.NET Core antiforgery token to same-origin write requests.</summary>
public sealed class AntiforgeryHttpClient(HttpClient httpClient)
{
    /// <summary>Sends a JSON write request with the required antiforgery header.</summary>
    public async Task<HttpResponseMessage> SendAsync<T>(HttpMethod method, string uri, T? value,
        CancellationToken cancellationToken = default)
    {
        var token = await httpClient.GetFromJsonAsync<AntiforgeryToken>("api/antiforgery/token",
            cancellationToken) ?? throw new HttpRequestException("Could not obtain an antiforgery token.");
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("RequestVerificationToken", token.Token);
        if (value is not null)
        {
            request.Content = JsonContent.Create(value);
        }

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private sealed record AntiforgeryToken(string Token);
}