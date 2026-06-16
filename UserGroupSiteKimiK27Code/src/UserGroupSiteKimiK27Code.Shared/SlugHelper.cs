using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace UserGroupSiteKimiK27Code.Shared;

public static partial class SlugHelper
{
    public static string ToSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";

        var normalized = title.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c) || c == ' ' || c == '-')
            {
                builder.Append(c);
            }
        }

        var slug = builder.ToString().ToLowerInvariant();
        slug = MyRegex().Replace(slug, "-").Trim('-');
        slug = Regex.Replace(slug, "-{2,}", "-");

        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9\-]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}