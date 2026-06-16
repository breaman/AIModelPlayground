using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK27Code.Data.Interfaces;
using UserGroupSiteKimiK27Code.Data.Models;
using UserGroupSiteKimiK27Code.Server.Services;

namespace UserGroupSiteKimiK27Code.Tests;

public class ServerTopicSuggestionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly ServerTopicSuggestionService _service;
    private readonly FakeUserService _userService;

    public ServerTopicSuggestionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _userManager = TestHelpers.CreateUserManager(_dbContext);
        _userService = new FakeUserService();
        _service = new ServerTopicSuggestionService(_dbContext, _userService, _userManager);
    }

    [Fact]
    public async Task VoteAsync_Toggles_Vote_And_Updates_Count()
    {
        var user = await TestHelpers.CreateSpeakerAsync(_dbContext, _userManager, "Voter", "One");
        _userService.UserIdValue = user.Id;
        var suggestion = await CreateSuggestionAsync(user.Id);

        var result = await _service.VoteAsync(suggestion.Id);
        Assert.NotNull(result);
        Assert.True(result.HasVoted);
        Assert.Equal(1, result.VoteCount);

        result = await _service.VoteAsync(suggestion.Id);
        Assert.NotNull(result);
        Assert.False(result.HasVoted);
        Assert.Equal(0, result.VoteCount);
    }

    [Fact]
    public async Task VolunteerAsync_Assigns_Current_User_As_Volunteer()
    {
        var suggester = await TestHelpers.CreateSpeakerAsync(_dbContext, _userManager, "Suggester", "One");
        var volunteer = await TestHelpers.CreateSpeakerAsync(_dbContext, _userManager, "Volunteer", "One");
        _userService.UserIdValue = volunteer.Id;
        var suggestion = await CreateSuggestionAsync(suggester.Id);

        var result = await _service.VolunteerAsync(suggestion.Id);
        Assert.NotNull(result);
        Assert.Equal(volunteer.Id, result.VolunteerSpeakerId);

        var second = await _service.VolunteerAsync(suggestion.Id);
        Assert.Null(second);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    private async Task<TopicSuggestion> CreateSuggestionAsync(int userId)
    {
        var suggestion = new TopicSuggestion
        {
            Title = "Test Topic",
            Description = "Description",
            SuggestedById = userId
        };
        _dbContext.TopicSuggestions.Add(suggestion);
        await _dbContext.SaveChangesAsync();
        return suggestion;
    }

    private sealed class FakeUserService : IUserService
    {
        public int UserIdValue { get; set; }
        public int UserId => UserIdValue;
    }
}