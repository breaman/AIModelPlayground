using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Server.Services;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;
using UserGroupSiteOpus5.Tests.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace UserGroupSiteOpus5.Tests;

public class ServerTopicSuggestionServiceTests
{
    [Fact]
    public async Task AMemberCanSuggestATopic()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        db.SignIn(member);

        var result = await service.CreateSuggestionAsync(new TopicSuggestionCreateModel
        {
            Title = "Source generators",
            Description = "How they work and when they help."
        });

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        (await db.DbContext.TopicSuggestions.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task AnAnonymousVisitorCannotSuggestATopic()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        db.SignOut();

        var result = await service.CreateSuggestionAsync(new TopicSuggestionCreateModel { Title = "Nope" });

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task ATitlelessSuggestionIsRejected()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        db.SignIn(member);

        var result = await service.CreateSuggestionAsync(new TopicSuggestionCreateModel { Title = "" });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Title is required.");
    }

    [Fact]
    public async Task AMemberCanVoteForASuggestion()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        var suggestionId = await CreateSuggestionAsync(db, member.Id);
        db.SignIn(member);

        (await service.VoteAsync(suggestionId)).Succeeded.ShouldBeTrue();
        (await db.DbContext.TopicSuggestionVotes.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task ASecondVoteFromTheSameMemberIsRejected()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        var suggestionId = await CreateSuggestionAsync(db, member.Id);
        db.SignIn(member);

        await service.VoteAsync(suggestionId);
        var second = await service.VoteAsync(suggestionId);

        second.Succeeded.ShouldBeFalse();
        second.Errors.ShouldContain("You have already voted for this topic.");
        (await db.DbContext.TopicSuggestionVotes.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task TheUniqueIndexRejectsADuplicateVoteThatBypassesTheService()
    {
        // The service check is a friendlier error; this is the guarantee behind it.
        using var db = new TestDatabase();
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        var suggestionId = await CreateSuggestionAsync(db, member.Id);

        db.DbContext.TopicSuggestionVotes.Add(new TopicSuggestionVote
        {
            TopicSuggestionId = suggestionId,
            UserId = member.Id
        });
        await db.DbContext.SaveChangesAsync();

        db.DbContext.TopicSuggestionVotes.Add(new TopicSuggestionVote
        {
            TopicSuggestionId = suggestionId,
            UserId = member.Id
        });

        await Should.ThrowAsync<DbUpdateException>(() => db.DbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DifferentMembersCanEachVoteOnce()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var first = await db.CreateUserAsync("first@test.local", "Fay");
        var second = await db.CreateUserAsync("second@test.local", "Sid");
        var suggestionId = await CreateSuggestionAsync(db, first.Id);

        db.SignIn(first);
        await service.VoteAsync(suggestionId);
        db.SignIn(second);
        await service.VoteAsync(suggestionId);

        (await db.DbContext.TopicSuggestionVotes.CountAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task AMemberCanVolunteerForAnUnclaimedTopic()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        var suggestionId = await CreateSuggestionAsync(db, member.Id);
        db.SignIn(member);

        (await service.VolunteerAsync(suggestionId)).Succeeded.ShouldBeTrue();

        var suggestion = await db.DbContext.TopicSuggestions.AsNoTracking().FirstAsync(x => x.Id == suggestionId);
        suggestion.VolunteerUserId.ShouldBe(member.Id);
    }

    [Fact]
    public async Task VolunteeringForAnAlreadyClaimedTopicIsRejected()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var first = await db.CreateUserAsync("first@test.local", "Fay");
        var second = await db.CreateUserAsync("second@test.local", "Sid");
        var suggestionId = await CreateSuggestionAsync(db, first.Id);

        db.SignIn(first);
        await service.VolunteerAsync(suggestionId);

        db.SignIn(second);
        var result = await service.VolunteerAsync(suggestionId);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Someone else has already volunteered to present this topic.");
    }

    [Fact]
    public async Task SuggestionsAreOrderedByVoteCountDescending()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        var voter = await db.CreateUserAsync("voter@test.local", "Val");

        var unpopular = await CreateSuggestionAsync(db, member.Id, "Unpopular");
        var popular = await CreateSuggestionAsync(db, member.Id, "Popular");

        db.SignIn(member);
        await service.VoteAsync(popular);
        db.SignIn(voter);
        await service.VoteAsync(popular);
        await service.VoteAsync(unpopular);

        var results = await service.GetSuggestionsAsync();

        results.Select(x => x.Title).ShouldBe(["Popular", "Unpopular"]);
        results[0].VoteCount.ShouldBe(2);
    }

    [Fact]
    public async Task TheListReportsWhetherTheCurrentMemberHasVoted()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var voter = await db.CreateUserAsync("voter@test.local", "Val");
        var other = await db.CreateUserAsync("other@test.local", "Otto");
        var suggestionId = await CreateSuggestionAsync(db, voter.Id);

        db.SignIn(voter);
        await service.VoteAsync(suggestionId);

        (await service.GetSuggestionsAsync())[0].HasCurrentUserVoted.ShouldBeTrue();

        db.SignIn(other);
        (await service.GetSuggestionsAsync())[0].HasCurrentUserVoted.ShouldBeFalse();
    }

    private static async Task<int> CreateSuggestionAsync(TestDatabase db, int userId, string title = "A topic")
    {
        var suggestion = new TopicSuggestion { Title = title, SuggestedByUserId = userId };
        db.DbContext.TopicSuggestions.Add(suggestion);
        await db.DbContext.SaveChangesAsync();
        return suggestion.Id;
    }

    private static ITopicSuggestionService CreateService(TestDatabase db)
    {
        return new ServerTopicSuggestionService(
            db.DbContext,
            db.GetService<IHttpContextAccessor>(),
            NullLogger<ServerTopicSuggestionService>.Instance);
    }
}
