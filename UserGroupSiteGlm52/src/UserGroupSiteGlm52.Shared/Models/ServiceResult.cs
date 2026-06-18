namespace UserGroupSiteGlm52.Shared.Models;

/// <summary>
/// Outcome of a mutating operation (create/update/vote/volunteer/role change)
/// that may fail with field-level or general errors. Used by both the server
/// services (DB-backed) and the client services (HTTP-backed) so the UI can
/// surface server validation consistently. <see cref="ErrorStatus"/> lets a
/// service signal the appropriate HTTP status (e.g. 403 for authorization).
/// </summary>
public sealed record ServiceResult
{
    public bool Succeeded { get; init; }

    /// <summary>HTTP status to report on failure (default 400; use 403 for authorization failures).</summary>
    public int ErrorStatus { get; init; } = 400;

    /// <summary>Non-field errors (e.g. authorization, not-found, concurrency).</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Field-keyed validation errors, matching ProblemDetails <c>errors</c>.</summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = new Dictionary<string, string[]>();

    public static ServiceResult Success() => new() { Succeeded = true };

    public static ServiceResult Failed(params string[] errors) => new() { Succeeded = false, Errors = errors };

    public static ServiceResult Forbidden(params string[] errors) => new() { Succeeded = false, ErrorStatus = 403, Errors = errors };

    public static ServiceResult Failed(IReadOnlyDictionary<string, string[]> validationErrors) =>
        new() { Succeeded = false, ValidationErrors = validationErrors };
}

/// <summary>Typed outcome carrying a value on success.</summary>
public sealed record ServiceResult<T>
{
    public bool Succeeded { get; init; }

    public T? Value { get; init; }

    public int ErrorStatus { get; init; } = 400;

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = new Dictionary<string, string[]>();

    public static ServiceResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public static ServiceResult<T> Failed(params string[] errors) => new() { Succeeded = false, Errors = errors };

    public static ServiceResult<T> Forbidden(params string[] errors) => new() { Succeeded = false, ErrorStatus = 403, Errors = errors };

    public static ServiceResult<T> NotFound(params string[] errors) => new() { Succeeded = false, ErrorStatus = 404, Errors = errors };

    public static ServiceResult<T> Failed(IReadOnlyDictionary<string, string[]> validationErrors) =>
        new() { Succeeded = false, ValidationErrors = validationErrors };
}