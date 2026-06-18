using System.Text;

namespace UserGroupSiteFable5.Shared.Helpers;

/// <summary>
/// Converts arbitrary text into a URL-friendly slug. Shared so the client can auto-fill
/// the slug from the title and the server can normalize/validate the same way.
/// </summary>
public static class SlugHelper
{
    /// <summary>
    /// Kebab-cases the input: lowercase, alphanumerics preserved, every other run of
    /// characters collapsed to a single hyphen, with no leading/trailing hyphens.
    /// </summary>
    /// <example>
    /// <code>SlugHelper.GenerateSlug("Intro to Blazor!") // "intro-to-blazor"</code>
    /// </example>
    public static string GenerateSlug(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        var previousWasHyphen = true; // suppress leading hyphens

        foreach (var character in input.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasHyphen = false;
            }
            else if (!previousWasHyphen)
            {
                builder.Append('-');
                previousWasHyphen = true;
            }
        }

        // Trim a trailing hyphen left by non-alphanumeric input endings.
        if (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }

        return builder.ToString();
    }
}