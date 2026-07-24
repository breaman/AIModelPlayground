using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGpt56Sol.Data.Models;

/// <summary>Represents a topic proposed by a community member.</summary>
public sealed class TopicSuggestion : FingerPrintEntityBase
{
    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(4_000)]
    public string? SupportingDetails { get; set; }

    public User Creator { get; set; } = null!;
    public int? VolunteerUserId { get; set; }
    public User? VolunteerUser { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ICollection<TopicVote> Votes { get; set; } = [];
}