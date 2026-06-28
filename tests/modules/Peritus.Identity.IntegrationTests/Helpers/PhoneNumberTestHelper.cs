using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Helpers;

public static class PhoneNumberTestHelper
{
    public static PhoneNumber Generate()
    {
        return new PhoneNumber($"+1{Random.Shared.Next(100000000, 999999999)}");
    }
}
