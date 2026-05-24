using Peritus.ApiClients.Abstractions;
using Peritus.Types.Tokens;
using Peritus.Web.Types;

namespace Peritus.Web.Services;

public class CookieTokenStorage(IHttpContextAccessor httpContextAccessor) : ITokenStorage
{
    public AccessToken? AccessToken
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Cookies[CookieName.AccessToken.Value];
            return string.IsNullOrEmpty(value) ? null : new AccessToken(value);
        }
    }

    public RefreshToken? RefreshToken
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Cookies[CookieName.RefreshToken.Value];
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

        context.Response.Cookies.Append(CookieName.AccessToken.Value, accessToken.Value, options);
        context.Response.Cookies.Append(CookieName.RefreshToken.Value, refreshToken.Value, options);
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

        context.Response.Cookies.Delete(CookieName.AccessToken.Value, options);
        context.Response.Cookies.Delete(CookieName.RefreshToken.Value, options);
    }
}
