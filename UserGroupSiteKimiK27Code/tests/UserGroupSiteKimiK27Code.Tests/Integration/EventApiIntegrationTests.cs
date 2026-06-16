using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using UserGroupSiteKimiK27Code.Data.Models;
using UserGroupSiteKimiK27Code.Shared.Dtos;

namespace UserGroupSiteKimiK27Code.Tests.Integration;

public class EventApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public EventApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("UseInMemoryDatabase", "true");
            builder.UseSetting("InMemoryDatabaseName", "IntegrationTests");
            builder.UseEnvironment("Development");
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPublishedEvents_Returns_Events_For_Anonymous_Users()
    {
        await ResetDatabaseAsync();
        await SeedPublishedEventAsync(1);

        var events = await _client.GetFromJsonAsync<List<EventListItemDto>>("api/events/published");

        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal("Integration Event", events[0].Title);
    }

    [Fact]
    public async Task GetEventBySlug_Returns_Published_Event()
    {
        await ResetDatabaseAsync();
        await SeedPublishedEventAsync(2);

        var evt = await _client.GetFromJsonAsync<EventDetailDto>("api/events/integration-event");

        Assert.NotNull(evt);
        Assert.Equal("Integration Event", evt.Title);
        Assert.True(evt.IsPublished);
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.TopicVotes.RemoveRange(dbContext.TopicVotes);
        dbContext.TopicSuggestions.RemoveRange(dbContext.TopicSuggestions);
        dbContext.EventSpeakers.RemoveRange(dbContext.EventSpeakers);
        dbContext.Events.RemoveRange(dbContext.Events);
        dbContext.Users.RemoveRange(dbContext.Users);
        dbContext.Roles.RemoveRange(dbContext.Roles);

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedPublishedEventAsync(int userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Users.Add(new User
        {
            Id = userId,
            UserName = $"speaker{userId}@test.local",
            Email = $"speaker{userId}@test.local",
            EmailConfirmed = true,
            FirstName = "Integration",
            LastName = "Speaker",
            MemberSince = DateTime.UtcNow
        });

        dbContext.Events.Add(new Event
        {
            Title = "Integration Event",
            Slug = "integration-event",
            ShortDescription = "Integration test event.",
            Description = "Description",
            EventDate = DateTime.UtcNow.AddDays(1),
            Location = "Room A",
            IsPublished = true,
            EventSpeakers = [new EventSpeaker { UserId = userId }]
        });

        await dbContext.SaveChangesAsync();
    }
}