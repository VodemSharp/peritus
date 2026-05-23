using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Types.Identity.Roles;
using Peritus.Types.Identity.Users;
using Peritus.Types.Localization;

namespace Peritus.Identity.Services.Abstractions;

public interface IUserService
{
    Task<UserId> CreateAsync(Email email, Password password, List<RoleName>? roleNames = null, Culture? culture = null,
        CancellationToken ct = default);

    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<User> GetByIdAsync(UserId userId, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(Email email, CancellationToken ct = default);
    Task<bool> AnyByEmailAsync(Email email, CancellationToken ct = default);
}
