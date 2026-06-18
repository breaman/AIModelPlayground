namespace UserGroupSiteGpt55.Shared.Events;

/// <summary>
/// Provides shared event validation rules used by services and tests.
/// </summary>
public static class EventValidation
{
    /// <summary>
    /// Validates draft and publish-specific event requirements except database uniqueness.
    /// </summary>
    public static IReadOnlyList<string> Validate(EventEditModel model)
    {
        var errors = new List<string>();
        var slug = SlugGenerator.Generate(model.Slug);

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            errors.Add("Slug is required.");
        }

        if (model.IsPublished)
        {
            if (string.IsNullOrWhiteSpace(model.MarkdownDescription))
            {
                errors.Add("Description is required before publishing.");
            }

            if (model.StartsAt is null)
            {
                errors.Add("Date and time are required before publishing.");
            }

            if (string.IsNullOrWhiteSpace(model.Location))
            {
                errors.Add("Location is required before publishing.");
            }

            if (model.SpeakerIds.Count == 0)
            {
                errors.Add("At least one speaker is required before publishing.");
            }
        }

        return errors;
    }
}