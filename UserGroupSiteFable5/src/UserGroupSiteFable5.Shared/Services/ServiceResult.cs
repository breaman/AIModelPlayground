namespace UserGroupSiteFable5.Shared.Services;

/// <summary>Categorizes a failed service call so API endpoints can map it to an HTTP status.</summary>
public enum ServiceErrorType
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

/// <summary>
/// Outcome of a write operation, shared by the client (HTTP) and server (database)
/// implementations of the data services so WASM pages get identical behavior in both modes.
/// </summary>
public record ServiceResult(bool Success, string? Error = null, ServiceErrorType ErrorType = ServiceErrorType.None)
{
    public static ServiceResult Ok { get; } = new(true);

    public static ServiceResult Fail(ServiceErrorType errorType, string error)
    {
        return new ServiceResult(false, error, errorType);
    }
}