namespace UserGroupSiteOpus48.Shared.Dtos;

/// <summary>
/// Result of a service operation that can fail with one or more human-readable validation errors.
/// Returned by services so the UI can show friendly messages without relying on exceptions.
/// </summary>
public record OperationResult(bool Success, IReadOnlyList<string> Errors)
{
    /// <summary>A successful result with no errors.</summary>
    public static OperationResult Ok() => new(true, []);

    /// <summary>A failed result carrying the supplied error messages.</summary>
    public static OperationResult Fail(params string[] errors) => new(false, errors);
}

/// <summary>
/// Result of a service operation that, on success, returns a value of type <typeparamref name="T"/>.
/// </summary>
public record OperationResult<T>(bool Success, IReadOnlyList<string> Errors, T? Value)
{
    /// <summary>A successful result carrying <paramref name="value"/>.</summary>
    public static OperationResult<T> Ok(T value) => new(true, [], value);

    /// <summary>A failed result carrying the supplied error messages.</summary>
    public static OperationResult<T> Fail(params string[] errors) => new(false, errors, default);
}