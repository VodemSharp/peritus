using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;

namespace Peritus.Identity.IntegrationTests.Infrastructure;

public class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public GoogleSignInPayload? Payload { get; set; }

    public Task<GoogleSignInPayload?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        return Task.FromResult(Payload);
    }
}
