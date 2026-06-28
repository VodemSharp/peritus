using OtpNet;

namespace Peritus.Identity.IntegrationTests.Helpers;

public static class TotpTestHelper
{
    public static string Generate(string secret)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.ComputeTotp();
    }
}
