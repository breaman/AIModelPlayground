using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Server.Services;
using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

using Xunit;

namespace UserGroupSiteFable5.Tests;

public class TopicServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private const int SuggesterId = 1;
    private const int VoterId = 2;
    private const int SecondVoterId = 3;
    private const int TopicId = 1;

    public TopicServiceTests()
    {
        _database.AddUser(SuggesterId, "suggester@example.com", "Sue", "Gester");
        _database.AddUser(VoterId, "voter@example.com");
        _database.AddUser(SecondVoterId, "second@example.com");

        _database.Context.TopicSuggestions.Add(new TopicSuggestion
        {
            Id = TopicId,
            Title = "Source Generators Deep Dive",
            SuggestedByUserId = SuggesterId
        });
        _database.Context.SaveChanges();
    }

    private TopicService CreateService(int currentUserId)
    {
        return new TopicService(_database.CreateContext(currentUserId), new FakeUserService(currentUserId));
    }

    [Fact]
    public async Task Vote_FirstTime_Succeeds()
    {
        var service = CreateService(VoterId);

        var result = await service.VoteAsync(TopicId);

        Assert.True(result.Success);
        var topics = await service.GetTopicsAsync();
        Assert.Equal(1, topics.Single().VoteCount);
        Assert.True(topics.Single().CurrentUserVoted);
    }

    [Fact]
    public async Task Vote_SecondTimeBySameUser_IsRejected()
    {
        var service = CreateService(VoterId);
        await service.VoteAsync(TopicId);

        var result = await service.VoteAsync(TopicId);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Vote_ByDifferentUsers_BothCount()
    {
        await CreateService(VoterId).VoteAsync(TopicId);
        await CreateService(SecondVoterId).VoteAsync(TopicId);

        var topics = await CreateService(SuggesterId).GetTopicsAsync();

        Assert.Equal(2, topics.Single().VoteCount);
        Assert.False(topics.Single().CurrentUserVoted);
    }

    [Fact]
    public async Task Volunteer_FirstClaim_Succeeds()
    {
        var service = CreateService(VoterId);

        var result = await service.VolunteerAsync(TopicId);

        Assert.True(result.Success);
        var topics = await service.GetTopicsAsync();
        Assert.Equal("voter@example.com", topics.Single().VolunteerName);
    }

    [Fact]
    public async Task Volunteer_WhenAlreadyClaimed_IsRejected()
    {
        await CreateService(VoterId).VolunteerAsync(TopicId);

        var result = await CreateService(SecondVoterId).VolunteerAsync(TopicId);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateTopic_WithoutTitle_FailsValidation()
    {
        var service = CreateService(SuggesterId);

        var result = await service.CreateTopicAsync(new TopicCreateDto { Title = "" });

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Topics_AreSortedByVoteCountDescending()
    {
        var context = _database.CreateContext(SuggesterId);
        context.TopicSuggestions.Add(new TopicSuggestion
        {
            Id = 2,
            Title = "A Popular Topic",
            SuggestedByUserId = SuggesterId
        });
        await context.SaveChangesAsync();

        await CreateService(VoterId).VoteAsync(2);
        await CreateService(SecondVoterId).VoteAsync(2);
        await CreateService(VoterId).VoteAsync(TopicId);

        var topics = await CreateService(SuggesterId).GetTopicsAsync();

        Assert.Equal([2, TopicId], topics.Select(t => t.Id).ToArray());
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}