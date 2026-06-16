using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK27Code.Data.Models;
using UserGroupSiteKimiK27Code.Shared;

namespace UserGroupSiteKimiK27Code.Server.Components.Pages.Admin;

[Authorize(Policy = "Admin")]
public partial class Users : ComponentBase
{
    [Inject] private UserManager<User> UserManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [SupplyParameterFromForm]
    private UserRolesForm? _form { get; set; }

    private string? _message;
    private bool _isSaving;
    private int _currentUserId;

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            var userId = UserManager.GetUserId(authState.User);
            _currentUserId = int.TryParse(userId, out var id) ? id : 0;
        }

        _form ??= await LoadFormAsync();
    }

    private async Task<UserRolesForm> LoadFormAsync()
    {
        var users = await Task.WhenAll((await UserManager.Users.ToListAsync())
            .Select(async u => new UserRoleEntry
            {
                Id = u.Id,
                FullName = $"{u.FirstName} {u.LastName}".Trim(),
                Email = u.Email ?? "",
                IsAdmin = await UserManager.IsInRoleAsync(u, Roles.Admin),
                IsSpeaker = await UserManager.IsInRoleAsync(u, Roles.Speaker)
            }));

        return new UserRolesForm { Users = users.ToList() };
    }

    private async Task SaveChangesAsync()
    {
        _isSaving = true;
        _message = null;

        foreach (var formUser in _form?.Users ?? [])
        {
            var user = await UserManager.FindByIdAsync(formUser.Id.ToString());
            if (user is null) continue;

            if (formUser.Id == _currentUserId)
            {
                // Prevent self-demotion from admin.
                formUser.IsAdmin = true;
            }

            await EnsureRoleAsync(user, Roles.Admin, formUser.IsAdmin);
            await EnsureRoleAsync(user, Roles.Speaker, formUser.IsSpeaker);
        }

        _form = await LoadFormAsync();
        _isSaving = false;
        _message = "User roles updated successfully.";
    }

    private async Task EnsureRoleAsync(User user, string role, bool shouldHaveRole)
    {
        var hasRole = await UserManager.IsInRoleAsync(user, role);
        if (shouldHaveRole && !hasRole)
        {
            await UserManager.AddToRoleAsync(user, role);
        }
        else if (!shouldHaveRole && hasRole)
        {
            await UserManager.RemoveFromRoleAsync(user, role);
        }
    }

    public sealed class UserRolesForm
    {
        public List<UserRoleEntry> Users { get; set; } = [];
    }

    public sealed class UserRoleEntry
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsAdmin { get; set; }
        public bool IsSpeaker { get; set; }
    }
}