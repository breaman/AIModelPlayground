using UserGroupSiteOpus5.Shared.Services;

namespace UserGroupSiteOpus5.Tests;

public class MarkdownRendererTests
{
    [Fact]
    public void ToHtmlRendersBasicMarkdown()
    {
        var html = MarkdownRenderer.ToHtml("## Heading\n\n- one\n- two");

        html.ShouldContain("<h2");
        html.ShouldContain("<li>one</li>");
    }

    [Fact]
    public void ToHtmlEscapesEmbeddedScriptTags()
    {
        // The whole reason DisableHtml() is on the pipeline: descriptions are user-authored and
        // rendered through a MarkupString, so raw HTML must never survive as markup.
        var html = MarkdownRenderer.ToHtml("Hello <script>alert('xss')</script>");

        html.ShouldNotContain("<script>");
        html.ShouldContain("&lt;script&gt;");
    }

    [Fact]
    public void ToHtmlEscapesInlineEventHandlerAttributes()
    {
        var html = MarkdownRenderer.ToHtml("<img src=x onerror=\"alert(1)\" />");

        html.ShouldNotContain("<img");
        html.ShouldContain("&lt;img");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToHtmlReturnsEmptyForNothingToRender(string? markdown)
    {
        MarkdownRenderer.ToHtml(markdown).ShouldBe("");
    }

    [Fact]
    public void ToPlainTextStripsMarkup()
    {
        var text = MarkdownRenderer.ToPlainText("## Heading\n\nSome **bold** text.");

        text.ShouldContain("Heading");
        text.ShouldContain("bold");
        text.ShouldNotContain("**");
    }
}
