namespace UserGroupSiteOpus5.Shared.Models;

/// <summary>
/// The outcome of a mutating service call.
/// </summary>
/// <remarks>
/// Both the server (direct database) and client (HTTP) implementations of a service return this,
/// so a caller reports failures the same way regardless of which one it got. Without it the
/// pre-rendered path would throw while the hydrated path returned a status code.
/// </remarks>
/// <example>
/// <code>
/// var result = await EventService.SaveEventAsync(model);
/// if (!result.Succeeded)
/// {
///     Toast.ShowError(string.Join(" ", result.Errors));
/// }
/// </code>
/// </example>
public record SaveResult
{
    /// <summary>Whether the operation completed successfully.</summary>
    public bool Succeeded { get; init; }

    /// <summary>Human-readable failure reasons. Empty when <see cref="Succeeded"/> is true.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Creates a successful result.</summary>
    public static SaveResult Success()
    {
        return new SaveResult { Succeeded = true };
    }

    /// <summary>Creates a failed result carrying the supplied messages.</summary>
    public static SaveResult Failure(params string[] errors)
    {
        return new SaveResult { Succeeded = false, Errors = errors };
    }

    /// <summary>Creates a failed result carrying the supplied messages.</summary>
    public static SaveResult Failure(IEnumerable<string> errors)
    {
        return new SaveResult { Succeeded = false, Errors = errors.ToList() };
    }
}

/// <summary>
/// The outcome of a mutating service call that produces a value, such as the id of a new record.
/// </summary>
/// <typeparam name="T">The type of the produced value.</typeparam>
public record SaveResult<T> : SaveResult
{
    /// <summary>The produced value. Meaningful only when <see cref="SaveResult.Succeeded"/> is true.</summary>
    public T? Value { get; init; }

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static SaveResult<T> Success(T value)
    {
        return new SaveResult<T> { Succeeded = true, Value = value };
    }

    /// <summary>Creates a failed result carrying the supplied messages.</summary>
    public static new SaveResult<T> Failure(params string[] errors)
    {
        return new SaveResult<T> { Succeeded = false, Errors = errors };
    }

    /// <summary>Creates a failed result carrying the supplied messages.</summary>
    public static new SaveResult<T> Failure(IEnumerable<string> errors)
    {
        return new SaveResult<T> { Succeeded = false, Errors = errors.ToList() };
    }
}
