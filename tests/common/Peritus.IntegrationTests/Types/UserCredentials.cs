using Peritus.Types.Identity.Users;

namespace Peritus.IntegrationTests.Types;

public readonly record struct UserCredentials(Email Email, Password Password)
{
    public static UserCredentials Create()
    {
        return new UserCredentials(
            new Email($"it+{Guid.CreateVersion7():N}@example.com"),
            new Password(Guid.CreateVersion7().ToString())
        );
    }
}
