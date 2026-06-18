using Microsoft.EntityFrameworkCore;

using UserGroupSiteOpus48.Data.Interfaces;
using UserGroupSiteOpus48.Data.Models;
using UserGroupSiteOpus48.Server.Services;

using Xunit;

namespace UserGroupSiteOpus48.Tests;

public class ServerTopicServiceTests
{
    private static ApplicationDbContext CreateDb(IUserService userService) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options, userService);

    private static async Task SeedTopicAsync(ApplicationDbContext db, int topicId, int suggestedBy)
    {
        db.TopicSuggestions.Add(new TopicSuggestion { Id = topicId, Title = "Topic", SuggestedByUserId = suggestedBy });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SecondVoteBySameUserIsRejectedAndCountStaysOne()
    {
        var user = new TestUserService(1);
        using var db = CreateDb(user);
        await SeedTopicAsync(db, 1, 1);
        var service = new ServerTopicService(db, user);

        var first = await service.VoteAsync(1);
        var second = await service.VoteAsync(1);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal(1, db.TopicVotes.Count(v => v.TopicSuggestionId == 1));
    }

    [Fact]
    public async Task DifferentUsersCanEachVoteOnce()
    {
        var user1 = new TestUserService(1);
        using var db = CreateDb(user1);
        await SeedTopicAsync(db, 1, 1);

        await new ServerTopicService(db, user1).VoteAsync(1);
        await new ServerTopicService(db, new TestUserService(2)).VoteAsync(1);

        Assert.Equal(2, db.TopicVotes.Count(v => v.TopicSuggestionId == 1));
    }

    [Fact]
    public async Task VolunteeringIsSingleOccupancy()
    {
        var user1 = new TestUserService(1);
        using var db = CreateDb(user1);
        await SeedTopicAsync(db, 1, 9);

        var firstVolunteer = await new ServerTopicService(db, user1).VolunteerAsync(1);
        var secondVolunteer = await new ServerTopicService(db, new TestUserService(2)).VolunteerAsync(1);

        Assert.True(firstVolunteer.Success);
        Assert.False(secondVolunteer.Success);
        Assert.Equal(1, db.TopicSuggestions.Single(t => t.Id == 1).VolunteerUserId);
    }
}