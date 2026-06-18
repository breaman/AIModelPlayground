using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteFable5.Server.Services;

/// <summary>Runs DataAnnotations validation (including <see cref="IValidatableObject"/>) server-side.</summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates the model the same way the WASM <c>EditForm</c> does, so both tiers
    /// enforce identical rules. Returns null when valid, otherwise a combined message.
    /// </summary>
    public static string? Validate(object model)
    {
        var results = new List<ValidationResult>();

        return Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true)
            ? null
            : string.Join(" ", results.Select(r => r.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
    }
}