using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Assertions;
using Peritus.Identity.IntegrationTests.Helpers;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class SignInTwoFactorTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignIn_With2FaEnabled_ReturnsTwoFactorTokenAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);
        await SetupTwoFactorAsync(api, _ct);

        // Act
        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        // Assert
        var result = ApiAssert.Success(signInResponse);
        Assert.True(result.RequiresTwoFactor);
        Assert.NotNull(result.TwoFactorToken);
        Assert.NotEmpty(result.TwoFactorToken);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
    }

    [Fact]
    public async Task TwoFactorSignIn_WithValidCode_ReturnsTokensAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);
        var setup = await SetupTwoFactorAsync(api, _ct);

        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        var twoFactorToken = signInResponse.Content!.TwoFactorToken!;
        var code = TotpTestHelper.Generate(setup.Secret);

        // Act
        var twoFactorSignInResponse = await api.TwoFactorSignInAsync(new SignInTwoFactorRequest
        {
            TwoFactorToken = twoFactorToken,
            Code = code
        }, _ct);

        // Assert
        var result = ApiAssert.Success(twoFactorSignInResponse);
        JwtAssert.Valid(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken.Value);
    }

    [Fact]
    public async Task TwoFactorSignIn_WithInvalidCode_ReturnsInvalidCodeErrorAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);
        await SetupTwoFactorAsync(api, _ct);

        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        var twoFactorToken = signInResponse.Content!.TwoFactorToken!;

        // Act
        var twoFactorSignInResponse = await api.TwoFactorSignInAsync(new SignInTwoFactorRequest
        {
            TwoFactorToken = twoFactorToken,
            Code = "000000"
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(twoFactorSignInResponse, nameof(SignInTwoFactorRequest.Code),
            "Invalid code.");
    }
}
