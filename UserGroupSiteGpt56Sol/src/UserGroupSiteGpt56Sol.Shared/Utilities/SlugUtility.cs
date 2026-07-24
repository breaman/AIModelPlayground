using System.Globalization;
using System.Text;

namespace UserGroupSiteGpt56Sol.Shared.Utilities;

/// <summary>Generates and normalizes stable URL slugs.</summary>
public static class SlugUtility
{
    /// <summary>Converts arbitrary text to a lowercase ASCII kebab-case slug.</summary>
    public static string Generate(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingDash = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                if (pendingDash && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                pendingDash = false;
            }
            else
            {
                pendingDash = builder.Length > 0;
            }
        }

        return builder.ToString();
    }

    /// <summary>Normalizes a browser-supplied slug before uniqueness comparisons.</summary>
    public static string Normalize(string value) => Generate(value);
}
