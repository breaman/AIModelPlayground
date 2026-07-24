using UserGroupSiteGpt56Sol.Shared.Utilities;

namespace UserGroupSiteGpt56Sol.Tests;

public sealed class SlugUtilityTests
{
    [Theory]
    [InlineData("Hello, Technical World!", "hello-technical-world")]
    [InlineData("  Déjà Vu  ", "deja-vu")]
    [InlineData("many---spaces", "many-spaces")]
    [InlineData("", "")]
    public void GenerateReturnsNormalizedKebabCase(string input, string expected)
    {
        Assert.Equal(expected, SlugUtility.Generate(input));
    }

    [Fact]
    public void NormalizeMakesEquivalentSlugsEqual()
    {
        Assert.Equal(SlugUtility.Normalize("New EVENT"), SlugUtility.Normalize("new-event"));
    }
}
