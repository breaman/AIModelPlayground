using System.Globalization;
using System.Text;

namespace UserGroupSiteOpus5.Shared.Common;

/// <summary>
/// Turns an event title into a URL-safe slug.
/// </summary>
/// <remarks>
/// Lives in Shared so the client's live "slug on blur" preview and the server's uniqueness check
/// produce byte-identical output; two implementations would eventually disagree on some edge case
/// and surprise the user at save time.
/// </remarks>
/// <example>
/// <code>
/// SlugGenerator.Generate("Minimal APIs in .NET 10!"); // "minimal-apis-in-net-10"
/// SlugGenerator.Generate("Café Résumé");              // "cafe-resume"
/// </code>
/// </example>
public static class SlugGenerator
{
    /// <summary>
    /// Produces a lowercase, hyphen-separated slug: diacritics are folded to their ASCII base
    /// letter, every run of non-alphanumeric characters becomes a single hyphen, and leading and
    /// trailing hyphens are trimmed.
    /// </summary>
    /// <param name="title">The source text. May be null or whitespace.</param>
    /// <param name="maxLength">Maximum slug length, defaulting to the storage limit.</param>
    /// <returns>The slug, or an empty string when <paramref name="title"/> yields no usable characters.</returns>
    public static string Generate(string? title, int maxLength = FieldLengths.Slug)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "";
        }

        // FormD splits "é" into "e" + combining accent, so the accent can be dropped by category.
        var normalized = title.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var lastWasHyphen = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                // ASCII-only output keeps slugs safe in URLs without percent-encoding.
                if (character > 127)
                {
                    continue;
                }

                builder.Append(char.ToLowerInvariant(character));
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        var slug = builder.ToString().Trim('-');

        if (slug.Length > maxLength)
        {
            // Trim again: truncation can leave a trailing hyphen mid-word.
            slug = slug[..maxLength].Trim('-');
        }

        return slug;
    }

    /// <summary>
    /// Appends a numeric suffix to <paramref name="baseSlug"/>, truncating the base if needed so
    /// the result still fits within <paramref name="maxLength"/>.
    /// </summary>
    /// <param name="baseSlug">The slug that collided.</param>
    /// <param name="suffix">The discriminator to append, starting at 2.</param>
    /// <param name="maxLength">Maximum slug length.</param>
    /// <returns>The suffixed slug, for example <c>my-event-2</c>.</returns>
    public static string WithSuffix(string baseSlug, int suffix, int maxLength = FieldLengths.Slug)
    {
        var suffixText = $"-{suffix}";

        if (baseSlug.Length + suffixText.Length > maxLength)
        {
            baseSlug = baseSlug[..(maxLength - suffixText.Length)].Trim('-');
        }

        return baseSlug + suffixText;
    }
}
