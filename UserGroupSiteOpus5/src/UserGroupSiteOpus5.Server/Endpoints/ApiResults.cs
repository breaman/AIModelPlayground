using UserGroupSiteOpus5.Shared.Models;

namespace UserGroupSiteOpus5.Server.Endpoints;

/// <summary>
/// Maps <see cref="SaveResult"/> onto HTTP responses so every endpoint reports failures the same
/// way.
/// </summary>
/// <remarks>
/// Failures come back as an RFC 9457 <c>ValidationProblemDetails</c> body, which the client
/// services unpack into the same <see cref="SaveResult"/> shape the server produced.
/// </remarks>
public static class ApiResults
{
    /// <summary>The key under which service errors are reported in the problem details body.</summary>
    public const string ErrorKey = "errors";

    /// <summary>Turns a valueless result into <c>200 OK</c> or <c>400</c> problem details.</summary>
    public static IResult ToHttpResult(this SaveResult result)
    {
        return result.Succeeded
            ? Results.Ok(result)
            : Results.ValidationProblem(ToProblemErrors(result));
    }

    /// <summary>Turns a value-carrying result into <c>200 OK</c> or <c>400</c> problem details.</summary>
    public static IResult ToHttpResult<T>(this SaveResult<T> result)
    {
        return result.Succeeded
            ? Results.Ok(result)
            : Results.ValidationProblem(ToProblemErrors(result));
    }

    private static Dictionary<string, string[]> ToProblemErrors(SaveResult result)
    {
        return new Dictionary<string, string[]>
        {
            [ErrorKey] = result.Errors.Count > 0 ? [.. result.Errors] : ["The request could not be completed."]
        };
    }
}
