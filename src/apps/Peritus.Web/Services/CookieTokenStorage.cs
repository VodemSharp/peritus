using Peritus.ApiClients.Abstractions;
using Peritus.Types.Tokens;

namespace Peritus.Web.Services;

public class CookieTokenStorage(IHttpContextAccessor httpContextAccessor) : ITokenStorage
{
    private const string AccessTokenCookie = "peritus_access_token";
    private const string RefreshTokenCookie = "peritus_refresh_token";

    public AccessToken? AccessToken
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Cookies[AccessTokenCookie];
            return string.IsNullOrEmpty(value) ? null : new AccessToken(value);
        }
    }

    public RefreshToken? RefreshToken
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Cookies[RefreshTokenCookie];
            return string.IsNullOrEmpty(value) ? null : new RefreshToken(value);
        }
    }

    public void SetTokens(AccessToken accessToken, RefreshToken refreshToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        var isDev = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDev,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        context.Response.Cookies.Append(AccessTokenCookie, accessToken.Value, options);
        context.Response.Cookies.Append(RefreshTokenCookie, refreshToken.Value, options);
    }

    public void ClearTokens()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        var options = new CookieOptions
        {
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };

        context.Response.Cookies.Delete(AccessTokenCookie, options);
        context.Response.Cookies.Delete(RefreshTokenCookie, options);
    }
}
