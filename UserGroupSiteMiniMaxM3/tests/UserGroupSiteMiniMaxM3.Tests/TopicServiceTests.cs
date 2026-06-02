using System.Security.Claims;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Topics;

using FluentAssertions;

namespace UserGroupSiteMiniMaxM3.Tests;

[Collection(nameof(SqliteDbCollection))]
public class TopicServiceTests
{
    private readonly SqliteDbFixture _fx;

    public TopicServiceTests(SqliteDbFixture fx) => _fx = fx;

    [Fact]
    public async Task CreateAsync_persists_topic_and_returns_summary()
    {
        var user = await TestHelpers.CreateUserAsync(_fx, "alice", "Alice", "Smith");

        var topic = await _fx.TopicService.CreateAsync(
            new TopicCreateDto { Title = "Blazor tips", Description = "a few things" },
            user.Id.ToString());

        topic.Id.Should().BeGreaterThan(0);
        topic.Title.Should().Be("Blazor tips");
        topic.SuggestedByDisplayName.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task CreateAsync_rejects_blank_title()
    {
        var user = await TestHelpers.CreateUserAsync(_fx, "bob");

        var act = () => _fx.TopicService.CreateAsync(
            new TopicCreateDto { Title = "" },
            user.Id.ToString());

        await act.Should().ThrowAsync<TopicValidationException>();
    }

    [Fact]
    public async Task ToggleVoteAsync_toggles_on_and_off()
    {
        var user = await TestHelpers.CreateUserAsync(_fx, "carol");
        var topic = await CreateTopicAsync("Topic", user);

        (await _fx.TopicService.ToggleVoteAsync(topic.Id, user.Id.ToString())).Should().BeTrue();
        (await _fx.TopicService.ToggleVoteAsync(topic.Id, user.Id.ToString())).Should().BeFalse();
    }

    [Fact]
    public async Task ToggleVolunteerAsync_reports_via_list()
    {
        var alice = await TestHelpers.CreateUserAsync(_fx, "alice2", "Alice", "Two");
        var bob = await TestHelpers.CreateUserAsync(_fx, "bob2");
        var topic = await CreateTopicAsync("T", alice);

        await _fx.TopicService.ToggleVolunteerAsync(topic.Id, bob.Id.ToString());

        var list = await _fx.TopicService.ListAsync(bob.Id.ToString());
        list.Should().HaveCount(1);
        list[0].VolunteerCount.Should().Be(1);
        list[0].UserHasVolunteered.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_rejects_non_owner_non_admin()
    {
        var owner = await TestHelpers.CreateUserAsync(_fx, "owner");
        var stranger = await TestHelpers.CreateUserAsync(_fx, "stranger");
        var topic = await CreateTopicAsync("mine", owner);

        var act = () => _fx.TopicService.DeleteAsync(topic.Id, stranger.Id.ToString(), isAdmin: false);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private async Task<TopicSummaryDto> CreateTopicAsync(string title, User owner)
    {
        return await _fx.TopicService.CreateAsync(new TopicCreateDto { Title = title }, owner.Id.ToString());
    }
}
