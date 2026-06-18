using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace UserGroupSiteGpt55.Shared.Events;

/// <summary>
/// Generates stable kebab-case slugs from titles.
/// </summary>
public static partial class SlugGenerator
{
    /// <summary>
    /// Converts a title into a lowercase URL slug.
    /// </summary>
    public static string Generate(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "";
        }

        var normalized = title.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            if (char.IsWhiteSpace(character) || character is '-' or '_' or '/')
            {
                builder.Append('-');
            }
        }

        return DuplicateDashesRegex().Replace(builder.ToString(), "-").Trim('-');
    }

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateDashesRegex();
}