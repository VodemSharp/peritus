using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Peritus.Guard.Claims;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Guard.Extensions;

public static class ClaimExtensions
{
    extension(ClaimsPrincipal principal)
    {
        public AccessTokenId GetAccessTokenId()
        {
            return new AccessTokenId(GetRequiredClaimGuid(principal, JwtRegisteredClaimNames.Jti));
        }

        public UserId GetUserId()
        {
            return new UserId(GetRequiredClaimGuid(principal, CustomClaimTypes.UserId));
        }
    }

    private static Guid GetRequiredClaimGuid(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirst(claimType)?.Value;
        return !Guid.TryParse(value, out var guid)
            ? throw new InvalidOperationException($"The '{claimType}' claim is missing or not a valid GUID.")
            : guid;
    }
}
