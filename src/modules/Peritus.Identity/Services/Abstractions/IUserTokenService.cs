using Peritus.FluentResults;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Services.Abstractions;

public interface IUserTokenService
{
    Task<UserTokenCreationResult> CreateAsync(UserId userId, UserTokenType type, TimeSpan expiry, string? value = null,
        CancellationToken ct = default);

    Task<FluentResult<UserTokenInfo>> RedeemAsync(UserId userId, UserTokenType type, string rawToken,
        CancellationToken ct = default);
}

public class UserTokenCreationResult
{
    public required UserTokenInfo Token { get; init; }
    public required string RawToken { get; init; }
}

public class UserTokenInfo
{
    public required UserId UserId { get; set; }
    public required UserTokenType Type { get; set; }
    public required string TokenHash { get; set; }
    public string? Value { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public required DateTime CreatedAt { get; set; }
}
