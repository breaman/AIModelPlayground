namespace UserGroupSiteMiniMaxM3.Shared.Models;

/// <summary>DTO used by the speaker picker (event editor).</summary>
/// <param name="Id">User id.</param>
/// <param name="DisplayName">"First Last" or user name when names are empty.</param>
public record SpeakerOption(int Id, string DisplayName);