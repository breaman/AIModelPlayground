using Microsoft.EntityFrameworkCore;

using UserGroupSiteGpt55.Data.Models;
using UserGroupSiteGpt55.Data.Models.Events;
using UserGroupSiteGpt55.Data.Models.Topics;

namespace UserGroupSiteGpt55.Tests;

public sealed class ApplicationDbContextModelTests
{
    [Fact]
    public void Model_ConfiguresUniqueEventSlug()
    {
        using var dbContext = CreateDbContext();

        var index = dbContext.Model.FindEntityType(typeof(UserGroupEvent))?
            .GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(UserGroupEvent.Slug)]));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Model_ConfiguresUniqueTopicVotePerUserPerTopic()
    {
        using var dbContext = CreateDbContext();

        var key = dbContext.Model.FindEntityType(typeof(TopicVote))?.FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Equal([nameof(TopicVote.TopicSuggestionId), nameof(TopicVote.UserId)], key.Properties.Select(p => p.Name));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UserGroupSiteGpt55Tests;Trusted_Connection=True;")
            .Options;

        return new ApplicationDbContext(options);
    }
}