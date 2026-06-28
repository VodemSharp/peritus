namespace Peritus.Identity.Types;

public class GoogleSignInPayload
{
    public required string Sub { get; init; }
    public required string Email { get; init; }
    public required bool EmailVerified { get; init; }
    public string? Name { get; init; }
    public string? Picture { get; init; }
}
