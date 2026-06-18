using System.Security.Claims;

using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Server.Endpoints;

/// <summary>Shared helpers for the minimal API endpoint groups.</summary>
public static class EndpointHelpers
{
    /// <summary>The authenticated user's integer id, or null when unauthenticated/malformed.</summary>
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        return int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
    }

    /// <summary>Maps a <see cref="ServiceResult"/> to the appropriate HTTP result.</summary>
    public static IResult ToHttpResult(this ServiceResult result)
    {
        return result switch
        {
            { Success: true } => Results.NoContent(),
            { ErrorType: ServiceErrorType.Validation } =>
                Results.Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
            { ErrorType: ServiceErrorType.Unauthorized } => Results.Unauthorized(),
            { ErrorType: ServiceErrorType.Forbidden } =>
                Results.Problem(detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            { ErrorType: ServiceErrorType.NotFound } => Results.NotFound(),
            { ErrorType: ServiceErrorType.Conflict } =>
                Results.Problem(detail: result.Error, statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(detail: result.Error)
        };
    }
}