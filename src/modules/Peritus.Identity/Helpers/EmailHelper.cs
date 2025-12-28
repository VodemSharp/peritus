using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Helpers;

public static class EmailHelper
{
    public static Email Normalize(Email email)
    {
        return new Email(email.Value.ToLower().Trim());
    }
}
