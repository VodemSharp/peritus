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
            return new AccessTokenId(
                Guid.Parse(principal.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value)
            );
        }

        public UserId GetUserId()
        {
            return new UserId(Guid.Parse(principal.Claims.Single(c => c.Type == CustomClaimTypes.UserId).Value));
        }
    }
}
