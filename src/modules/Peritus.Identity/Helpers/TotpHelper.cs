using System.Security.Cryptography;
using System.Text;
using OtpNet;
using Peritus.Types.Identity.Users;
using Peritus.Types.Jwt;

namespace Peritus.Identity.Helpers;

public static class TotpHelper
{
    public static string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public static string GenerateQrCodeUri(Email email, string secret, JwtIssuer issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}";
    }

    public static bool ValidateCode(string secret, string code)
    {
        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key);
        return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
    }

    public static string[] GenerateRecoveryCodes(int count = 10)
    {
        var codes = new string[count];
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = new byte[8];

        for (var i = 0; i < count; i++)
        {
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var code = new StringBuilder(8);
            foreach (var b in bytes)
            {
                code.Append(chars[b % chars.Length]);
            }

            codes[i] = code.ToString();
        }

        return codes;
    }
}
