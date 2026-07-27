using System.Net;
using System.Net.Http.Json;

using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Tests.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

namespace UserGroupSiteOpus5.Tests;

[Collection(IntegrationTestCollection.Name)]
public class ApiAuthorizationTests : IAsyncLifetime
{
    private readonly ApiTestFactory _factory = new();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task PublishedEventsAreReadableAnonymously()
    {
        using var client = _factory.CreateClientAs(userId: null);

        var response = await client.GetAsync("/api/events");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/events/manage")]
    [InlineData("/api/speakers")]
    [InlineData("/api/topics")]
    [InlineData("/api/admin/users")]
    public async Task ProtectedRoutesRejectAnonymousCallers(string route)
    {
        using var client = _factory.CreateClientAs(userId: null);

        var response = await client.GetAsync(route);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnOrdinaryMemberIsForbiddenFromTheAdminRoutes()
    {
        var memberId = await _factory.CreateUserAsync("member@test.local");
        using var client = _factory.CreateClientAs(memberId);

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnAdminCanReadTheMemberList()
    {
        var adminId = await _factory.CreateUserAsync("admin@test.local", RoleNames.Admin);
        using var client = _factory.CreateClientAs(adminId, RoleNames.Admin);

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<UserListItem>>()).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task AnOrdinaryMemberIsForbiddenFromTheManageRoute()
    {
        var memberId = await _factory.CreateUserAsync("member@test.local");
        using var client = _factory.CreateClientAs(memberId);

        var response = await client.GetAsync("/api/events/manage");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ASpeakerGets404OnAnotherSpeakersEvent()
    {
        var assignedId = await _factory.CreateUserAsync("assigned@test.local", RoleNames.Speaker);
        var otherId = await _factory.CreateUserAsync("other@test.local", RoleNames.Speaker);
        var eventId = await CreateEventAsync("Theirs", "theirs", assignedId);

        using var client = _factory.CreateClientAs(otherId, RoleNames.Speaker);

        // The edit endpoint reports a meeting the caller may not edit as missing, so an outsider
        // cannot use it to enumerate drafts.
        (await client.GetAsync($"/api/events/{eventId}/edit")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetFromJsonAsync<bool>($"/api/events/{eventId}/can-edit")).ShouldBeFalse();
    }

    [Fact]
    public async Task AnAssignedSpeakerCanLoadTheirOwnEventForEditing()
    {
        var speakerId = await _factory.CreateUserAsync("speaker@test.local", RoleNames.Speaker);
        var eventId = await CreateEventAsync("Mine", "mine", speakerId);

        using var client = _factory.CreateClientAs(speakerId, RoleNames.Speaker);

        var response = await client.GetAsync($"/api/events/{eventId}/edit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<EventEditModel>())!.Title.ShouldBe("Mine");
    }

    [Fact]
    public async Task ASpeakerCannotUpdateAnotherSpeakersEvent()
    {
        var assignedId = await _factory.CreateUserAsync("assigned@test.local", RoleNames.Speaker);
        var otherId = await _factory.CreateUserAsync("other@test.local", RoleNames.Speaker);
        var eventId = await CreateEventAsync("Theirs", "theirs", assignedId);

        using var client = _factory.CreateClientAs(otherId, RoleNames.Speaker);

        var response = await client.PutAsJsonAsync($"/api/events/{eventId}",
            new EventEditModel { Id = eventId, Title = "Hijacked", Slug = "hijacked" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("permission");
    }

    [Fact]
    public async Task AnAdminCanCreateAnEvent()
    {
        var adminId = await _factory.CreateUserAsync("admin@test.local", RoleNames.Admin);
        using var client = _factory.CreateClientAs(adminId, RoleNames.Admin);

        var response = await client.PostAsJsonAsync("/api/events",
            new EventEditModel { Title = "New meeting", Slug = "new-meeting" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<SaveResult<int>>())!.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task ASpeakerCannotCreateAnEvent()
    {
        var speakerId = await _factory.CreateUserAsync("speaker@test.local", RoleNames.Speaker);
        using var client = _factory.CreateClientAs(speakerId, RoleNames.Speaker);

        var response = await client.PostAsJsonAsync("/api/events",
            new EventEditModel { Title = "Sneaky", Slug = "sneaky" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("administrator");
    }

    [Fact]
    public async Task AnAdminCannotRemoveTheirOwnAdminRoleThroughTheApi()
    {
        // The disabled checkbox in the UI is cosmetic; this is the request that bypasses it.
        var adminId = await _factory.CreateUserAsync("admin@test.local", RoleNames.Admin);
        using var client = _factory.CreateClientAs(adminId, RoleNames.Admin);

        var response = await client.PutAsJsonAsync($"/api/admin/users/{adminId}/roles",
            new UserRoleUpdateModel(adminId, IsAdmin: false, IsSpeaker: false));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("cannot remove your own Admin role");
    }

    [Fact]
    public async Task TheRouteIdWinsOverAMismatchedBodyOnRoleUpdates()
    {
        // A body naming a different user must not retarget the change away from the route.
        var adminId = await _factory.CreateUserAsync("admin@test.local", RoleNames.Admin);
        var targetId = await _factory.CreateUserAsync("target@test.local");
        using var client = _factory.CreateClientAs(adminId, RoleNames.Admin);

        var response = await client.PutAsJsonAsync($"/api/admin/users/{targetId}/roles",
            new UserRoleUpdateModel(adminId, IsAdmin: false, IsSpeaker: true));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await _factory.WithScopeAsync(async provider =>
        {
            var dbContext = provider.GetRequiredService<ApplicationDbContext>();
            var adminRoleId = dbContext.Roles.Single(x => x.Name == RoleNames.Admin).Id;
            dbContext.UserRoles.Any(x => x.UserId == adminId && x.RoleId == adminRoleId).ShouldBeTrue();
            await Task.CompletedTask;
        });
    }

    [Fact]
    public async Task StateChangingCallsWithoutTheClientHeaderAreRejected()
    {
        // A cross-site form post rides the auth cookie but cannot set a custom header, so the
        // filter is what stops it.
        var adminId = await _factory.CreateUserAsync("admin@test.local", RoleNames.Admin);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, adminId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, RoleNames.Admin);

        var response = await client.PostAsJsonAsync("/api/events",
            new EventEditModel { Title = "Forged", Slug = "forged" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Missing client header");
    }

    [Fact]
    public async Task ADraftIsNotReadableAnonymouslyBySlug()
    {
        await CreateEventAsync("Draft", "draft-event", speakerUserId: null, isPublished: false);

        using var client = _factory.CreateClientAs(userId: null);

        (await client.GetAsync("/api/events/draft-event")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnAnonymousVisitorCannotVoteOnATopic()
    {
        using var client = _factory.CreateClientAs(userId: null);

        var response = await client.PostAsync("/api/topics/1/vote", content: null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    private async Task<int> CreateEventAsync(string title, string slug, int? speakerUserId, bool isPublished = false)
    {
        var eventId = 0;

        await _factory.WithScopeAsync(async provider =>
        {
            var dbContext = provider.GetRequiredService<ApplicationDbContext>();
            var meeting = new Event { Title = title, Slug = slug, IsPublished = isPublished };

            if (speakerUserId is { } userId)
            {
                meeting.Speakers.Add(new EventSpeaker { UserId = userId });
            }

            dbContext.Events.Add(meeting);
            await dbContext.SaveChangesAsync();
            eventId = meeting.Id;
        });

        return eventId;
    }
}
