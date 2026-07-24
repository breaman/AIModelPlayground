using UserGroupSiteGpt56Sol.Shared.Utilities;

namespace UserGroupSiteGpt56Sol.Tests;

public sealed class MarkdownRendererTests
{
    [Fact]
    public void ToSafeHtmlRendersSupportedMarkdown()
    {
        var html = MarkdownRenderer.ToSafeHtml("# Heading\nA **bold** idea\n- first");

        Assert.Contains("<h1>Heading</h1>", html, StringComparison.Ordinal);
        Assert.Contains("<strong>bold</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<ul><li>first</li></ul>", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<script>alert('x')</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    public void ToSafeHtmlEncodesUnsafeMarkup(string markdown)
    {
        var html = MarkdownRenderer.ToSafeHtml(markdown);

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;", html, StringComparison.Ordinal);
    }
}
