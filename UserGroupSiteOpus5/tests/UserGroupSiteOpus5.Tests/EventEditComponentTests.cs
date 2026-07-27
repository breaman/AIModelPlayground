using UserGroupSiteOpus5.Client.Components.Pages;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace UserGroupSiteOpus5.Tests;

public class EventEditComponentTests : BunitContext
{
    private readonly IEventService _eventService = Substitute.For<IEventService>();
    private readonly IToastService _toastService = Substitute.For<IToastService>();

    public EventEditComponentTests()
    {
        _eventService.GetAvailableSpeakersAsync()
            .Returns(Task.FromResult<IReadOnlyList<EventSpeakerInfo>>([new EventSpeakerInfo(1, "Sam", "Speaker")]));

        Services.AddSingleton(_eventService);
        Services.AddSingleton(_toastService);
    }

    [Fact]
    public void TheSlugIsGeneratedWhenTheTitleLosesFocusAndTheSlugIsEmpty()
    {
        _eventService.GenerateUniqueSlugAsync("Minimal APIs", null)
            .Returns(Task.FromResult("minimal-apis"));

        var component = Render<EventEdit>();

        component.Find("#Model\\.Title").Change("Minimal APIs");
        component.Find("#Model\\.Title").Blur();

        component.Find("#Model\\.Slug").GetAttribute("value").ShouldBe("minimal-apis");
    }

    [Fact]
    public void ASlugTheUserTypedIsNeverOverwritten()
    {
        // Overwriting would silently change a meeting's address and break existing links.
        var component = Render<EventEdit>();

        component.Find("#Model\\.Slug").Change("hand-written-slug");
        component.Find("#Model\\.Title").Change("A Completely Different Title");
        component.Find("#Model\\.Title").Blur();

        component.Find("#Model\\.Slug").GetAttribute("value").ShouldBe("hand-written-slug");
        _eventService.DidNotReceive().GenerateUniqueSlugAsync(Arg.Any<string>(), Arg.Any<int?>());
    }

    [Fact]
    public void NoSlugIsGeneratedForAnEmptyTitle()
    {
        var component = Render<EventEdit>();

        component.Find("#Model\\.Title").Blur();

        _eventService.DidNotReceive().GenerateUniqueSlugAsync(Arg.Any<string>(), Arg.Any<int?>());
    }

    [Fact]
    public void ThePreviewTabRendersTheDescriptionAsMarkdown()
    {
        var component = Render<EventEdit>();

        component.Find("#Model\\.Description").Change("## Agenda\n\nSomething **important**.");
        ClickPreviewTab(component);

        var preview = component.Find(".markdown-body");
        preview.InnerHtml.ShouldContain("<h2");
        preview.InnerHtml.ShouldContain("<strong>important</strong>");
    }

    [Fact]
    public void ThePreviewTabEscapesEmbeddedHtml()
    {
        var component = Render<EventEdit>();

        component.Find("#Model\\.Description").Change("<script>alert('xss')</script>");
        ClickPreviewTab(component);

        component.Find(".markdown-body").InnerHtml.ShouldNotContain("<script>");
    }

    [Fact]
    public void ThePreviewTabSaysSoWhenThereIsNothingToPreview()
    {
        var component = Render<EventEdit>();

        ClickPreviewTab(component);

        component.Markup.ShouldContain("Nothing to preview yet.");
    }

    [Fact]
    public void PublishingWithMissingDataSurfacesValidationMessagesOnSubmit()
    {
        var component = Render<EventEdit>();

        component.Find("#Model\\.Title").Change("Incomplete meeting");
        component.Find("#Model\\.Slug").Change("incomplete-meeting");
        component.Find("#Model\\.IsPublished").Change(true);
        component.Find("form").Submit();

        // The ValidationSummary carries the alert-danger class from the markup; the readiness hint
        // next to the publish toggle is alert-warning, so this selector picks out the summary.
        var summary = component.Find(".alert-danger").TextContent;
        summary.ShouldContain("A description is required to publish an event.");
        summary.ShouldContain("A date and time is required to publish an event.");
        summary.ShouldContain("A location is required to publish an event.");
        summary.ShouldContain("At least one speaker is required to publish an event.");

        _eventService.DidNotReceive().SaveEventAsync(Arg.Any<EventEditModel>());
    }

    [Fact]
    public void TheReadinessHintListsWhatIsStillMissing()
    {
        var component = Render<EventEdit>();

        component.Find("#Model\\.Title").Change("Incomplete meeting");

        component.Markup.ShouldContain("Not ready to publish");
        component.Markup.ShouldContain("a description");
        component.Markup.ShouldContain("at least one speaker");
    }

    [Fact]
    public void TheReadinessHintDisappearsOnceEverythingIsSupplied()
    {
        var component = Render<EventEdit>();

        component.Find("#Model\\.Title").Change("Complete meeting");
        component.Find("#Model\\.Slug").Change("complete-meeting");
        component.Find("#Model\\.Description").Change("All about it.");
        component.Find("#Model\\.Location").Change("Community Hall");
        component.Find("#Model\\.EventDateTime").Change("2026-09-01T18:30");
        component.Find("#speaker-1").Change(true);

        component.Markup.ShouldNotContain("Not ready to publish");
    }

    [Fact]
    public void ADraftWithOnlyATitleAndSlugSaves()
    {
        _eventService.SaveEventAsync(Arg.Any<EventEditModel>())
            .Returns(Task.FromResult(SaveResult<int>.Success(7)));

        var component = Render<EventEdit>();

        component.Find("#Model\\.Title").Change("Draft meeting");
        component.Find("#Model\\.Slug").Change("draft-meeting");
        component.Find("form").Submit();

        _eventService.Received(1).SaveEventAsync(Arg.Is<EventEditModel>(x =>
            x!.Title == "Draft meeting" && x.Slug == "draft-meeting" && !x.IsPublished));
    }

    [Fact]
    public void ASaveFailureIsReportedAsAToastRatherThanSwallowed()
    {
        _eventService.SaveEventAsync(Arg.Any<EventEditModel>())
            .Returns(Task.FromResult(SaveResult<int>.Failure("The slug 'taken' is already in use by another event.")));

        var component = Render<EventEdit>();

        component.Find("#Model\\.Title").Change("Duplicate");
        component.Find("#Model\\.Slug").Change("taken");
        component.Find("form").Submit();

        _toastService.Received(1).ShowError(
            Arg.Is<string>(x => x!.Contains("already in use")),
            Arg.Any<string>());
    }

    [Fact]
    public void SelectingASpeakerAddsThemToTheModel()
    {
        _eventService.SaveEventAsync(Arg.Any<EventEditModel>())
            .Returns(Task.FromResult(SaveResult<int>.Success(1)));

        var component = Render<EventEdit>();

        component.Find("#Model\\.Title").Change("Meeting");
        component.Find("#Model\\.Slug").Change("meeting");
        component.Find("#speaker-1").Change(true);
        component.Find("form").Submit();

        _eventService.Received(1).SaveEventAsync(Arg.Is<EventEditModel>(x => x!.SpeakerUserIds.Contains(1)));
    }

    /// <summary>Clicks the Preview tab, which is the second button in the description tab strip.</summary>
    private static void ClickPreviewTab(IRenderedComponent<EventEdit> component)
    {
        component.FindAll("button.nav-link")[1].Click();
    }
}
