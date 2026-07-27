using UserGroupSiteOpus5.Shared.Common;

namespace UserGroupSiteOpus5.Client.Services;

/// <summary>
/// Attaches the custom client header that the API's state-changing endpoints require.
/// </summary>
/// <remarks>
/// Applied as a <see cref="DelegatingHandler"/> on the shared <see cref="HttpClient"/> so no
/// individual call site can forget it. See <see cref="ApiHeaders.RequestedWith"/> for why the
/// header exists.
/// </remarks>
public class ClientRequestHeaderHandler : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(ApiHeaders.RequestedWith))
        {
            request.Headers.Add(ApiHeaders.RequestedWith, ApiHeaders.RequestedWithValue);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
