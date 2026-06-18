using System.Net.Http.Json;

using UserGroupSiteGlm52.Shared.Models;

namespace UserGroupSiteGlm52.Client.Services;

/// <summary>
/// Translates Minimal API responses into <see cref="ServiceResult"/> values,
/// parsing RFC 9457 ProblemDetails into field-level or general errors so the UI
/// can surface them consistently. Shared by the client service implementations.
/// </summary>
internal static class ClientResults
{
    /// <summary>Read a typed success value, or a failed result from a ProblemDetails error body.</summary>
    public static async Task<ServiceResult<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>();
            return ServiceResult<T>.Success(value!);
        }

        return await FailedAsync<T>(response);
    }

    /// <summary>Read a void success, or a failed result from a ProblemDetails error body.</summary>
    public static async Task<ServiceResult> ReadVoidAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return ServiceResult.Success();
        }

        return await FailedVoidAsync(response);
    }

    private static async Task<ServiceResult<T>> FailedAsync<T>(HttpResponseMessage response)
    {
        var problem = await ReadProblemAsync(response);
        return ServiceResultFactory.Failed<T>(problem.Errors, problem.GeneralErrors);
    }

    private static async Task<ServiceResult> FailedVoidAsync(HttpResponseMessage response)
    {
        var problem = await ReadProblemAsync(response);
        if (problem.Errors is { Count: > 0 })
        {
            return ServiceResult.Failed(problem.Errors);
        }

        return ServiceResult.Failed(problem.GeneralErrors.ToArray());
    }

    private static async Task<ParsedProblem> ReadProblemAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResult>();
            if (problem is not null)
            {
                var general = new List<string>();
                if (!string.IsNullOrWhiteSpace(problem.Title))
                {
                    general.Add(problem.Title);
                }

                if (!string.IsNullOrWhiteSpace(problem.Detail))
                {
                    general.Add(problem.Detail);
                }

                return new ParsedProblem(problem.Errors ?? new Dictionary<string, string[]>(), general);
            }
        }
        catch
        {
            // Fall through to the generic message if the body isn't JSON.
        }

        return new ParsedProblem(new Dictionary<string, string[]>(),
            [$"Request failed with status {(int)response.StatusCode}."]);
    }

    /// <summary>Add an overload accepting validation errors plus general errors.</summary>
    private sealed record ParsedProblem(IReadOnlyDictionary<string, string[]> Errors, IReadOnlyList<string> GeneralErrors);
}

/// <summary>Extension: build a typed failure from both validation and general errors.</summary>
internal static class ServiceResultFactory
{
    public static ServiceResult<T> Failed<T>(IReadOnlyDictionary<string, string[]> validationErrors, IReadOnlyList<string> generalErrors)
    {
        if (validationErrors.Count > 0)
        {
            return ServiceResult<T>.Failed(validationErrors);
        }

        return generalErrors.Count > 0
            ? ServiceResult<T>.Failed(generalErrors.ToArray())
            : ServiceResult<T>.Failed("The request could not be processed.");
    }
}