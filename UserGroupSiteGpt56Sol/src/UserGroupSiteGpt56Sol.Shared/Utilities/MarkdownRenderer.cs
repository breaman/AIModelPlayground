using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace UserGroupSiteGpt56Sol.Shared.Utilities;

/// <summary>Renders a deliberately small Markdown subset after HTML encoding all input.</summary>
public static partial class MarkdownRenderer
{
    /// <summary>Converts Markdown to sanitized HTML without allowing raw HTML.</summary>
    public static string ToSafeHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var output = new StringBuilder();
        var inList = false;
        foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.TrimEnd();
            var isListItem = line.StartsWith("- ", StringComparison.Ordinal);
            if (inList && !isListItem)
            {
                output.Append("</ul>");
                inList = false;
            }

            if (isListItem)
            {
                if (!inList)
                {
                    output.Append("<ul>");
                    inList = true;
                }

                output.Append("<li>").Append(RenderInline(line[2..])).Append("</li>");
            }
            else if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                output.Append("<h3>").Append(RenderInline(line[4..])).Append("</h3>");
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                output.Append("<h2>").Append(RenderInline(line[3..])).Append("</h2>");
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                output.Append("<h1>").Append(RenderInline(line[2..])).Append("</h1>");
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                output.Append("<p>").Append(RenderInline(line)).Append("</p>");
            }
        }

        if (inList)
        {
            output.Append("</ul>");
        }

        return output.ToString();
    }

    /// <summary>HTML-encodes inline text before adding a safe formatting subset.</summary>
    private static string RenderInline(string value)
    {
        var encoded = WebUtility.HtmlEncode(value);
        encoded = CodeRegex().Replace(encoded, "<code>$1</code>");
        encoded = BoldRegex().Replace(encoded, "<strong>$1</strong>");
        encoded = ItalicRegex().Replace(encoded, "<em>$1</em>");
        return encoded;
    }

    [GeneratedRegex("`([^`]+)`")]
    private static partial Regex CodeRegex();

    [GeneratedRegex("\\*\\*([^*]+)\\*\\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex("(?<!\\*)\\*([^*]+)\\*(?!\\*)")]
    private static partial Regex ItalicRegex();
}
