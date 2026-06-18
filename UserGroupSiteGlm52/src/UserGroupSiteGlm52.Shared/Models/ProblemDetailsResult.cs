namespace UserGroupSiteGlm52.Shared.Models;

/// <summary>
/// Minimal shape matching RFC 9457 ProblemDetails / ASP.NET ValidationProblemDetails,
/// used by the client services to parse error responses from the Minimal API.
/// <c>Errors</c> is populated for validation (400) responses.
/// </summary>
public sealed class ProblemDetailsResult
{
    public string? Type { get; set; }

    public string? Title { get; set; }

    public int Status { get; set; }

    public string? Detail { get; set; }

    public Dictionary<string, string[]>? Errors { get; set; }
}