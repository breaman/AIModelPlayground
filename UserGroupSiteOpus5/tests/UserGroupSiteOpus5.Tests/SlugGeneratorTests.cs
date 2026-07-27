using UserGroupSiteOpus5.Shared.Common;

namespace UserGroupSiteOpus5.Tests;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Minimal APIs in .NET 10", "minimal-apis-in-net-10")]
    [InlineData("UPPERCASE TITLE", "uppercase-title")]
    [InlineData("Trailing punctuation!!!", "trailing-punctuation")]
    [InlineData("  Leading and trailing  ", "leading-and-trailing")]
    [InlineData("Multiple   spaces", "multiple-spaces")]
    [InlineData("Already-hyphenated-title", "already-hyphenated-title")]
    [InlineData("C# & F# interop", "c-f-interop")]
    public void GenerateProducesLowercaseHyphenatedSlugs(string title, string expected)
    {
        SlugGenerator.Generate(title).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Café Résumé", "cafe-resume")]
    [InlineData("Naïve Über Ångström", "naive-uber-angstrom")]
    public void GenerateFoldsDiacriticsToAscii(string title, string expected)
    {
        SlugGenerator.Generate(title).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("日本語")]
    public void GenerateReturnsEmptyWhenNothingUsableRemains(string? title)
    {
        SlugGenerator.Generate(title).ShouldBe("");
    }

    [Fact]
    public void GenerateTruncatesToTheMaximumLength()
    {
        var slug = SlugGenerator.Generate(new string('a', 300));

        slug.Length.ShouldBe(FieldLengths.Slug);
    }

    [Fact]
    public void GenerateDoesNotLeaveATrailingHyphenAfterTruncation()
    {
        // The 200th character lands on a space, so a naive truncation would end with a hyphen.
        var title = new string('a', FieldLengths.Slug) + " tail";

        SlugGenerator.Generate(title).ShouldNotEndWith("-");
    }

    [Fact]
    public void WithSuffixAppendsTheDiscriminator()
    {
        SlugGenerator.WithSuffix("my-event", 2).ShouldBe("my-event-2");
        SlugGenerator.WithSuffix("my-event", 13).ShouldBe("my-event-13");
    }

    [Fact]
    public void WithSuffixKeepsTheResultWithinTheMaximumLength()
    {
        var baseSlug = new string('a', FieldLengths.Slug);

        var result = SlugGenerator.WithSuffix(baseSlug, 2);

        result.Length.ShouldBeLessThanOrEqualTo(FieldLengths.Slug);
        result.ShouldEndWith("-2");
    }
}
