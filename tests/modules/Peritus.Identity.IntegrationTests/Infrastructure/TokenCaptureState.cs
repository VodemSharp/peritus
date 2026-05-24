using Peritus.Identity.Types;

namespace Peritus.Identity.IntegrationTests.Infrastructure;

public class TokenCaptureState
{
    private readonly Dictionary<UserTokenType, string> _tokens = new();

    public void Capture(UserTokenType type, string rawToken)
    {
        _tokens[type] = rawToken;
    }

    public string? Get(UserTokenType type)
    {
        return _tokens.TryGetValue(type, out var token) ? token : null;
    }

    public void Reset()
    {
        _tokens.Clear();
    }
}
