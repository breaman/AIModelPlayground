using Bunit;

using System.Security.Claims;

using Microsoft.Extensions.DependencyInjection;

using UserGroupSiteGpt56Sol.Client.Components.Pages;
using UserGroupSiteGpt56Sol.Server.Components.Layout;
using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Tests;

public sealed class ComponentTests
{
    [Fact]
    public void AuthenticatedHeaderShowsFirstNameAccountAndLogout()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("fallback@example.com");
        authorization.SetClaims(new Claim("FirstName", "Ada"));

        var component = context.Render<NavMenu>();

        Assert.Contains("Welcome, Ada", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Manage Account", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Log out", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventEditorGeneratesSlugOnBlurAndRendersSafePreview()
    {
        using var context = CreateContext(AppRoles.Admin);
        context.Services.AddSingleton<IEventService>(new FakeEventService());

        var component = context.Render<EditEvent>();
        component.Find("#event-title").Change("A Déjà Vu Session!");
        component.Find("#event-title").Blur();

        Assert.Equal("a-deja-vu-session", component.Find("#event-slug").GetAttribute("value"));

        component.Find("#event-description").Change("# Safe\n<script>alert(1)</script>");
        component.FindAll("button").Single(button => button.TextContent.Trim() == "Preview").Click();

        Assert.Contains("<h1>Safe</h1>", component.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", component.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventEditorShowsConditionalPublishValidation()
    {
        using var context = CreateContext(AppRoles.Admin);
        context.Services.AddSingleton<IEventService>(new FakeEventService());
        var component = context.Render<EditEvent>();

        component.Find("#event-title").Change("Publish me");
        component.Find("#event-title").Blur();
        component.Find("#event-published").Change(true);
        component.Find("form").Submit();

        Assert.Contains("A description is required to publish.", component.Markup, StringComparison.Ordinal);
        Assert.Contains("At least one speaker is required to publish.", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentAdministratorAdminControlIsDisabled()
    {
        using var context = CreateContext(AppRoles.Admin);
        context.Services.AddSingleton<IUserAdministrationService>(new FakeUserAdministrationService());

        var component = context.Render<AdminUsers>();
        var checkbox = component.Find("#admin-7");

        Assert.True(checkbox.HasAttribute("disabled"));
        Assert.Contains("cannot remove your own Admin role", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TopicWithCurrentUserVoteShowsRemoveVoteStateAndHidesVolunteerAction()
    {
        using var context = CreateContext();
        context.Services.AddSingleton<ITopicService>(new FakeTopicService());

        var component = context.Render<Topics>();

        Assert.Contains("Remove vote", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Alex Speaker", component.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(component.FindAll("button"),
            button => button.TextContent.Trim().Equals("Volunteer", StringComparison.Ordinal));
    }

    /// <summary>Creates a bUnit context with authenticated state, persistence, and toast support.</summary>
    private static BunitContext CreateContext(params string[] roles)
    {
        var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("Test User");
        authorization.SetRoles(roles);
        context.AddBunitPersistentComponentState();
        context.Services.AddSingleton<IToastService>(new FakeToastService());
        return context;
    }

    private sealed class FakeEventService : IEventService
    {
        public Task<IReadOnlyList<EventSummaryDto>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventSummaryDto>>([]);

        public Task<EventDetailDto?> GetPublishedBySlugAsync(string slug,
            CancellationToken cancellationToken = default) => Task.FromResult<EventDetailDto?>(null);

        public Task<IReadOnlyList<EventAdminListItemDto>> GetManageListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventAdminListItemDto>>([]);

        public Task<EventEditRequest?> GetForEditAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventEditRequest?>(new EventEditRequest());

        public Task<IReadOnlyList<SpeakerOptionDto>> GetSpeakerOptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SpeakerOptionDto>>([]);

        public Task<ServiceResult<int>> SaveAsync(int? id, EventEditRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceResult<int>.Success(id ?? 1));
    }

    private sealed class FakeUserAdministrationService : IUserAdministrationService
    {
        public Task<IReadOnlyList<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserAdminDto>>(
                [new UserAdminDto(7, "Current Admin", "admin@example.com", [AppRoles.Admin], true)]);

        public Task<ServiceResult> UpdateRolesAsync(int userId, UpdateUserRolesRequest request,
            CancellationToken cancellationToken = default) => Task.FromResult(ServiceResult.Success());
    }

    private sealed class FakeTopicService : ITopicService
    {
        public Task<IReadOnlyList<TopicSuggestionDto>> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TopicSuggestionDto>>(
                [new TopicSuggestionDto(3, "Modern C#", null, "Test User", DateTime.UtcNow, 2, true,
                    "Alex Speaker")]);

        public Task<ServiceResult<int>> CreateAsync(CreateTopicRequest request,
            CancellationToken cancellationToken = default) => Task.FromResult(ServiceResult<int>.Success(3));

        public Task<ServiceResult> VoteAsync(int topicId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceResult.Success());

        public Task<ServiceResult> UnvoteAsync(int topicId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceResult.Success());

        public Task<ServiceResult> VolunteerAsync(int topicId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceResult.Success());
    }

    private sealed class FakeToastService : IToastService
    {
        public event Action<ToastMessage>? OnToastAdded;

        public void ShowSuccess(string message, string? heading = null) =>
            OnToastAdded?.Invoke(new ToastMessage(Guid.NewGuid(), message, ToastType.Success, heading));

        public void ShowError(string message, string? heading = null) =>
            OnToastAdded?.Invoke(new ToastMessage(Guid.NewGuid(), message, ToastType.Error, heading));

        public void ShowWarning(string message, string? heading = null) =>
            OnToastAdded?.Invoke(new ToastMessage(Guid.NewGuid(), message, ToastType.Warning, heading));

        public void ShowInfo(string message, string? heading = null) =>
            OnToastAdded?.Invoke(new ToastMessage(Guid.NewGuid(), message, ToastType.Info, heading));
    }
}
