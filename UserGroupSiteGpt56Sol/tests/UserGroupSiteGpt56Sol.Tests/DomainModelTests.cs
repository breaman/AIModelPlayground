using UserGroupSiteGpt56Sol.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteGpt56Sol.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void EventSlugAndJoinConstraintsAreUnique()
    {
        using var context = CreateContext();

        var eventType = context.Model.FindEntityType(typeof(Event))!;
        var speakerType = context.Model.FindEntityType(typeof(EventSpeaker))!;

        Assert.True(eventType.GetIndexes().Single(index =>
            index.Properties.Single().Name == nameof(Event.NormalizedSlug)).IsUnique);
        Assert.Equal([nameof(EventSpeaker.EventId), nameof(EventSpeaker.UserId)],
            speakerType.FindPrimaryKey()!.Properties.Select(property => property.Name));
    }

    [Fact]
    public void TopicVoteHasCompositeUniquenessAndVolunteerIsOptional()
    {
        using var context = CreateContext();

        var voteType = context.Model.FindEntityType(typeof(TopicVote))!;
        var topicType = context.Model.FindEntityType(typeof(TopicSuggestion))!;

        Assert.Equal([nameof(TopicVote.TopicSuggestionId), nameof(TopicVote.UserId)],
            voteType.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.True(voteType.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(TopicVote.TopicSuggestionId), nameof(TopicVote.UserId)])).IsUnique);
        Assert.True(topicType.FindProperty(nameof(TopicSuggestion.VolunteerUserId))!.IsNullable);
    }

    /// <summary>Creates a context for inspecting relational model metadata without opening a connection.</summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ModelOnly;Trusted_Connection=True")
            .Options;
        return new ApplicationDbContext(options);
    }
}
