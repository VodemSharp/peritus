namespace Peritus.Web.Types;

public readonly record struct CookieName(string Value)
{
    public static readonly CookieName AccessToken = new("peritus_access_token");
    public static readonly CookieName RefreshToken = new("peritus_refresh_token");

    public override string ToString()
    {
        return Value;
    }
}
