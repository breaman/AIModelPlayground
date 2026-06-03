using System.Text;

namespace UserGroupSiteOpus48.Shared.Text;

/// <summary>
/// Converts arbitrary text (typically an event title) into a URL-friendly kebab-case slug.
/// Shared so the editor's auto-fill and any server-side normalization stay consistent.
/// </summary>
public static class SlugGenerator
{
    /// <summary>
    /// Produces a lowercase, hyphen-separated slug: letters/digits are kept, runs of any other
    /// characters collapse to a single hyphen, and leading/trailing hyphens are trimmed.
    /// </summary>
    /// <param name="input">The source text, e.g. an event title.</param>
    /// <returns>The kebab-case slug, or an empty string when the input has no usable characters.</returns>
    public static string ToKebabCase(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        var lastWasHyphen = false;

        foreach (var ch in input.Trim())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                // Collapse any run of separators/punctuation into a single hyphen.
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
