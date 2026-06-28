using Peritus.ApiContracts.Identity;
using Peritus.IntegrationTests.Types;
using Peritus.Types.Tokens;

namespace Peritus.Identity.IntegrationTests.Types;

public readonly record struct AuthenticatedUser(
    UserCredentials Credentials,
    AuthTokenPair Tokens,
    IIdentityApi Api);
