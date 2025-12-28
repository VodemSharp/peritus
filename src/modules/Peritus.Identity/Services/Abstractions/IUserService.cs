using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Types.Identity.Roles;
using Peritus.Types.Identity.Users;
using Peritus.Types.Localization;

namespace Peritus.Identity.Services.Abstractions;

public interface IUserService
{
    Task<bool> AnyAsync(Email email);
    Task<User?> GetOrDefaultAsync(Email email);
    Task<UserId> CreateAsync(Email email, Password password, List<RoleName>? roleNames = null, Culture? culture = null);
    Task UpdateAsync(User user);
}
