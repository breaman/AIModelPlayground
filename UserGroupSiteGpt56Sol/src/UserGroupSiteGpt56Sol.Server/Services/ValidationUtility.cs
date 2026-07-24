using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGpt56Sol.Server.Services;

/// <summary>Runs DataAnnotations validation for service entry points.</summary>
internal static class ValidationUtility
{
    /// <summary>Returns validation failures grouped by their member names.</summary>
    public static IReadOnlyDictionary<string, string[]> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Message: result.ErrorMessage ?? "Invalid value.")))
            .GroupBy(item => item.Member, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Message).Distinct().ToArray(),
                StringComparer.Ordinal);
    }
}