using System.Security.Cryptography;
using System.Text;

namespace Peritus.Identity.Helpers;

public static class TokenHasher
{
    public static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

}
