using System.Text;
using System.Text.RegularExpressions;

namespace UserGroupSiteQwen35.Shared.Utilities;

public static partial class SlugHelper
{
    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        // Convert to lowercase
        var slug = title.ToLowerInvariant();

        // Replace spaces with hyphens
        slug = slug.Replace(" ", "-");

        // Remove special characters using regex
        slug = SlugRegex().Replace(slug, "");

        // Remove consecutive hyphens
        slug = MultipleHyphensRegex().Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphensRegex();
}
