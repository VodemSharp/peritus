using Microsoft.IdentityModel.JsonWebTokens;
using Peritus.Types.Tokens;

namespace Peritus.Identity.IntegrationTests.Assertions;

public static class JwtAssert
{
    public static void Valid(AccessToken accessToken)
    {
        var token = accessToken.Value;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);

        var handler = new JsonWebTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var jwt = handler.ReadJsonWebToken(token);
        Assert.NotNull(jwt);
        Assert.False(string.IsNullOrWhiteSpace(jwt.Alg));
        Assert.NotEmpty(jwt.Claims);
    }
}
