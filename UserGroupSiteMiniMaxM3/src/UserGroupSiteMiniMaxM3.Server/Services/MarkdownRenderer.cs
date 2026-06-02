using System.Text;

using Markdig;

namespace UserGroupSiteMiniMaxM3.Server.Services;

/// <summary>
/// Renders user-supplied markdown to safe HTML for the event detail page.
/// Uses Markdig with default CommonMark features but strips raw HTML and
/// applies a conservative tag/attribute allow-list to defend against XSS.
/// </summary>
public static class MarkdownRenderer
{
    // The HTML elements we allow after markdown -> HTML conversion. Anything
    // outside this set is removed before the result is returned. The list
    // mirrors the most common event-description building blocks.
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "em", "b", "i", "u", "s", "code", "pre",
        "blockquote", "ul", "ol", "li",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "a", "img",
        "table", "thead", "tbody", "tr", "th", "td",
        "hr",
    };

    // Allowed attributes on the elements above. Only href/src on links/images,
    // and alt/title on images. Everything else is stripped.
    private static readonly Dictionary<string, HashSet<string>> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = new(StringComparer.OrdinalIgnoreCase) { "href", "title" },
        ["img"] = new(StringComparer.OrdinalIgnoreCase) { "src", "alt", "title" },
    };

    // URL schemes permitted on <a href="..."> and <img src="...">. Anything
    // else (e.g. javascript:) is removed.
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "mailto", "tel",
    };

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>Renders the given markdown to a safe HTML string.</summary>
    /// <param name="markdown">Raw markdown. May be null or whitespace; in that case an empty string is returned.</param>
    /// <returns>Sanitized HTML.</returns>
    public static string Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var raw = Markdig.Markdown.ToHtml(markdown, Pipeline);
        return Sanitize(raw);
    }

    /// <summary>
    /// Walks the HTML and removes disallowed tags, attributes, and URL schemes.
    /// Implemented as a small allow-list parser rather than a regex so we don't
    /// depend on the HtmlAgilityPack or AngleSharp package.
    /// </summary>
    private static string Sanitize(string html)
    {
        var sb = new StringBuilder(html.Length);
        var i = 0;
        while (i < html.Length)
        {
            var c = html[i];
            if (c == '<')
            {
                var end = html.IndexOf('>', i);
                if (end < 0)
                {
                    // Unterminated tag: drop the rest of the buffer.
                    break;
                }
                var tag = html.Substring(i + 1, end - i - 1).Trim();
                ProcessTag(tag, sb);
                i = end + 1;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }

    private static void ProcessTag(string raw, StringBuilder sb)
    {
        // Comments and CDATA: drop.
        if (raw.StartsWith("!--", StringComparison.Ordinal))
        {
            return;
        }

        var isClosing = raw.StartsWith('/');
        var body = isClosing ? raw[1..].Trim() : raw.TrimEnd('/').Trim();

        // Doctype / processing instructions: drop.
        if (body.StartsWith('!') || body.StartsWith('?'))
        {
            return;
        }

        var tagEnd = 0;
        while (tagEnd < body.Length && !char.IsWhiteSpace(body[tagEnd]))
        {
            tagEnd++;
        }
        var tagName = body[..tagEnd].ToLowerInvariant();
        if (!AllowedTags.Contains(tagName))
        {
            return;
        }

        if (isClosing)
        {
            sb.Append("</").Append(tagName).Append('>');
            return;
        }

        sb.Append('<').Append(tagName);
        var attrSource = body[tagEnd..];
        if (attrSource.Length > 0)
        {
            var allowed = AllowedAttributes.TryGetValue(tagName, out var a) ? a : new HashSet<string>();
            foreach (var attr in ExtractAttributes(attrSource))
            {
                if (!allowed.Contains(attr.Name))
                {
                    continue;
                }
                if (attr.Name is "href" or "src" && !IsSafeUrl(attr.Value))
                {
                    continue;
                }
                sb.Append(' ').Append(attr.Name).Append("=\"").Append(Escape(attr.Value)).Append('"');
            }
        }
        sb.Append('>');
    }

    private static IEnumerable<(string Name, string Value)> ExtractAttributes(string source)
    {
        var i = 0;
        while (i < source.Length)
        {
            while (i < source.Length && char.IsWhiteSpace(source[i]))
            {
                i++;
            }
            if (i >= source.Length)
            {
                yield break;
            }

            var nameStart = i;
            while (i < source.Length && !char.IsWhiteSpace(source[i]) && source[i] != '=')
            {
                i++;
            }
            var name = source[nameStart..i].ToLowerInvariant();
            if (name.Length == 0)
            {
                break;
            }

            while (i < source.Length && char.IsWhiteSpace(source[i]))
            {
                i++;
            }
            string value = string.Empty;
            if (i < source.Length && source[i] == '=')
            {
                i++;
                while (i < source.Length && char.IsWhiteSpace(source[i]))
                {
                    i++;
                }
                if (i < source.Length && (source[i] == '"' || source[i] == '\''))
                {
                    var quote = source[i];
                    i++;
                    var valStart = i;
                    while (i < source.Length && source[i] != quote)
                    {
                        i++;
                    }
                    value = source[valStart..i];
                    if (i < source.Length)
                    {
                        i++;
                    }
                }
                else
                {
                    var valStart = i;
                    while (i < source.Length && !char.IsWhiteSpace(source[i]))
                    {
                        i++;
                    }
                    value = source[valStart..i];
                }
            }
            yield return (name, value);
        }
    }

    private static bool IsSafeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }
        var colon = url.IndexOf(':');
        if (colon < 0)
        {
            // Relative URL — allowed.
            return true;
        }
        var scheme = url[..colon];
        return AllowedSchemes.Contains(scheme);
    }

    private static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;");
}