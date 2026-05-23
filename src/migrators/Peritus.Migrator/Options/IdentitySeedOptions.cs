using Peritus.Types.Identity.Users;

namespace Peritus.Migrator.Options;

public class IdentitySeedOptions
{
    public Email AdminEmail { get; set; }
    public Password AdminPassword { get; set; }
}
