using UserGroupSiteKimiK27Code.Shared;

namespace UserGroupSiteKimiK27Code.Tests;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Leading and trailing spaces  ", "leading-and-trailing-spaces")]
    [InlineData("Special @#$% Characters", "special-characters")]
    [InlineData("Café & Résumé", "cafe-resume")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    [InlineData("", "")]
    [InlineData("A", "a")]
    public void ToSlug_Returns_Expected_Slug(string title, string expected)
    {
        var slug = SlugHelper.ToSlug(title);
        Assert.Equal(expected, slug);
    }
}