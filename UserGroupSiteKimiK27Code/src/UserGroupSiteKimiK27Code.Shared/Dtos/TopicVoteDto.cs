namespace UserGroupSiteKimiK27Code.Shared.Dtos;

public class TopicVoteDto
{
    public int TopicSuggestionId { get; set; }
    public int VoteCount { get; set; }
    public bool HasVoted { get; set; }
}