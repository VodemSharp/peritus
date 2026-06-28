using Peritus.Identity.Types;

namespace Peritus.Identity.Services.Abstractions;

public interface IGoogleTokenValidator
{
    Task<GoogleSignInPayload?> ValidateAsync(string idToken, CancellationToken ct = default);
}
