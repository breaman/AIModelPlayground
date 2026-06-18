using System.Globalization;
using System.Text;

namespace UserGroupSiteGlm52.Shared.Services;

/// <summary>
/// Converts titles to URL-friendly kebab-case slugs. Used by the event editor's
/// <c>@onblur</c> auto-fill and as a server fallback when the slug is empty.
/// </summary>
public static class SlugHelper
{
    /// <summary>Normalize <paramref name="text"/> to a kebab-case slug.</summary>
    public static string ToSlug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Decompose accents/diacritics then drop the combining marks.
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(ch);
        }

        var stripped = builder.ToString().Trim().ToLowerInvariant();

        // Replace any run of non-alphanumeric characters with a single hyphen.
        var slug = new StringBuilder(stripped.Length);
        var needsHyphen = false;
        foreach (var ch in stripped)
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (needsHyphen && slug.Length > 0)
                {
                    slug.Append('-');
                    needsHyphen = false;
                }

                slug.Append(ch);
            }
            else
            {
                needsHyphen = true;
            }
        }

        return slug.ToString().Trim('-');
    }
}