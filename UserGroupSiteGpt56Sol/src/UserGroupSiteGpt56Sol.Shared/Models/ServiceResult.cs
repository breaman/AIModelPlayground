namespace UserGroupSiteGpt56Sol.Shared.Models;

/// <summary>Represents a consistent service operation result.</summary>
public sealed record ServiceResult(bool Succeeded, string? Message = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public static ServiceResult Success(string? message = null) => new(true, message);
    public static ServiceResult Failure(string message) => new(false, message);
    public static ServiceResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(false, "Please correct the highlighted fields.", errors);
}

/// <summary>Represents a consistent service operation result with a response value.</summary>
public sealed record ServiceResult<T>(bool Succeeded, T? Value = default, string? Message = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public static ServiceResult<T> Success(T value, string? message = null) => new(true, value, message);
    public static ServiceResult<T> Failure(string message) => new(false, default, message);
    public static ServiceResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(false, default, "Please correct the highlighted fields.", errors);
}
