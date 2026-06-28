namespace Peritus.Identity.IntegrationTests.Types;

public readonly record struct TwoFactorSetup(string Secret, string[] RecoveryCodes);
