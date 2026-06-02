using System.Text.RegularExpressions;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;

namespace UserGroupSiteMiniMaxM3.Data.Services;

/// <summary>Result of validating an <see cref="GroupEvent"/> input DTO.</summary>
public class EventValidationResult
{
    /// <summary>Field-keyed error messages. Key is the property name (Title, Slug, etc.).</summary>
    public Dictionary<string, string[]> Errors { get; } = [];

    /// <summary>True when there are no errors.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Adds an error for a field.</summary>
    public void AddError(string field, string message)
    {
        if (Errors.TryGetValue(field, out var list))
        {
            Errors[field] = [.. list, message];
        }
        else
        {
            Errors[field] = [message];
        }
    }
}

/// <summary>
/// Encapsulates the rules for saving a <see cref="GroupEvent"/>. Pulled out of the service
/// so it can be unit-tested in isolation.
/// </summary>
public class EventValidator
{
    /// <summary>Validates the edit DTO against the published-event requirements.</summary>
    /// <param name="input">The form input.</param>
    /// <param name="slugIsUnique">Callback returning false when the slug is already taken by another event.</param>
    /// <param name="nowUtc">Current UTC time, injected for deterministic tests.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<EventValidationResult> ValidateAsync(
        EventEditDto input,
        Func<string, int, Task<bool>> slugIsUnique,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = new EventValidationResult();

        // Title and Slug are always required.
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            result.AddError(nameof(input.Title), "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(input.Slug))
        {
            result.AddError(nameof(input.Slug), "Slug is required.");
        }
        else if (!Regex.IsMatch(input.Slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            result.AddError(nameof(input.Slug), "Slug must be lowercase kebab-case (letters, digits, and dashes).");
        }
        else
        {
            var isUnique = await slugIsUnique(input.Slug, input.Id).WaitAsync(cancellationToken);
            if (!isUnique)
            {
                result.AddError(nameof(input.Slug), "Slug is already in use by another event.");
            }
        }

        // Published events have additional requirements.
        if (input.IsPublished)
        {
            if (string.IsNullOrWhiteSpace(input.Description))
            {
                result.AddError(nameof(input.Description), "Description is required for published events.");
            }

            if (string.IsNullOrWhiteSpace(input.Location))
            {
                result.AddError(nameof(input.Location), "Location is required for published events.");
            }

            // EventDateTime defaults to DateTime.MinValue when the form value is empty; treat that as missing.
            if (input.EventDateTime == default || input.EventDateTime < nowUtc.AddYears(-1))
            {
                result.AddError(nameof(input.EventDateTime), "Event date/time is required for published events.");
            }

            if (input.SpeakerIds.Count == 0)
            {
                result.AddError(nameof(input.SpeakerIds), "At least one speaker is required for published events.");
            }
        }

        return result;
    }
}