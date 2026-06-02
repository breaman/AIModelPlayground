using Microsoft.EntityFrameworkCore;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Shared.Models;

namespace UserGroupSiteMiniMaxM3.Data.Services;

/// <summary>Read-only lookup of users for the speaker picker.</summary>
public interface IUserLookupService
{
    /// <summary>Returns all users (id + display name) for selection in the editor.</summary>
    Task<IReadOnlyList<SpeakerOption>> GetSpeakerOptionsAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class UserLookupService(ApplicationDbContext db) : IUserLookupService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeakerOption>> GetSpeakerOptionsAsync(CancellationToken cancellationToken = default)
    {
        var users = await db.Users.AsNoTracking().OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync(cancellationToken);
        return users.Select(u => new SpeakerOption(u.Id, DisplayName(u))).ToList();
    }

    private static string DisplayName(User u)
    {
        var name = $"{u.FirstName} {u.LastName}".Trim();
        return string.IsNullOrEmpty(name) ? (u.UserName ?? "User") : name;
    }
}