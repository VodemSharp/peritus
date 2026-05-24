using System.Net;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using Peritus.ApiContracts.Identity.Accounts;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class TwoFactorTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task EnableTwoFactor_ReturnsSecretAndQrUriAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        // Act
        var response = await api.EnableTwoFactorAsync(_ct);

        // Assert
        var result = ApiAssert.Success(response);
        Assert.False(string.IsNullOrWhiteSpace(result.Secret));
        Assert.False(string.IsNullOrWhiteSpace(result.QrCodeUri));
        Assert.Contains(result.Secret, result.QrCodeUri);
    }

    [Fact]
    public async Task VerifyTwoFactorSetup_WithValidCode_Enables2FaAndReturnsRecoveryCodesAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var enableResponse = await api.EnableTwoFactorAsync(_ct);
        var secret = enableResponse.Content!.Secret;

        var code = GenerateTotpCode(secret);

        // Act
        var verifyResponse = await api.ConfirmTwoFactorSetupAsync(new VerifyTwoFactorRequest
        {
            Code = code
        }, _ct);

        // Assert
        var result = ApiAssert.Success(verifyResponse);
        Assert.NotEmpty(result.RecoveryCodes);
        Assert.Equal(10, result.RecoveryCodes.Length);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.True(user.TwoFactorEnabled);
    }

    [Fact]
    public async Task SignIn_With2FaEnabled_ReturnsTwoFactorTokenAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var enableResponse = await api.EnableTwoFactorAsync(_ct);
        var secret = enableResponse.Content!.Secret;
        var code = GenerateTotpCode(secret);

        await api.ConfirmTwoFactorSetupAsync(new VerifyTwoFactorRequest
        {
            Code = code
        }, _ct);

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
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var enableResponse = await api.EnableTwoFactorAsync(_ct);
        var secret = enableResponse.Content!.Secret;
        var code = GenerateTotpCode(secret);

        await api.ConfirmTwoFactorSetupAsync(new VerifyTwoFactorRequest
        {
            Code = code
        }, _ct);

        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        var twoFactorToken = signInResponse.Content!.TwoFactorToken!;
        var code2 = GenerateTotpCode(secret);

        // Act
        var twoFactorSignInResponse = await api.TwoFactorSignInAsync(new TwoFactorSignInRequest
        {
            TwoFactorToken = twoFactorToken,
            Code = code2
        }, _ct);

        // Assert
        var result = ApiAssert.Success(twoFactorSignInResponse);
        JwtAssert.Valid(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken.Value);
    }

    [Fact]
    public async Task RecoveryCodeSignIn_WithValidCode_ReturnsTokensAndConsumesCodeAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var enableResponse = await api.EnableTwoFactorAsync(_ct);
        var secret = enableResponse.Content!.Secret;
        var code = GenerateTotpCode(secret);

        var verifyResponse = await api.ConfirmTwoFactorSetupAsync(new VerifyTwoFactorRequest
        {
            Code = code
        }, _ct);
        var recoveryCode = verifyResponse.Content!.RecoveryCodes[0];

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        var twoFactorToken = signInResponse.Content!.TwoFactorToken!;

        // Act
        var recoveryCodeSignInResponse = await api.RecoveryCodeSignInAsync(new RecoveryCodeSignInRequest
        {
            TwoFactorToken = twoFactorToken,
            Code = recoveryCode
        }, _ct);

        // Assert
        var result = ApiAssert.Success(recoveryCodeSignInResponse);
        JwtAssert.Valid(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken.Value);

        var redeemedCode = await db.UserRecoveryCodes
            .Where(r => r.UserId == user.Id && r.RedeemedAt != null)
            .SingleAsync(_ct);

        Assert.NotNull(redeemedCode);
    }

    [Fact]
    public async Task TwoFactorSignIn_WithInvalidCode_ReturnsInvalidCodeErrorAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var enableResponse = await api.EnableTwoFactorAsync(_ct);
        var secret = enableResponse.Content!.Secret;
        var code = GenerateTotpCode(secret);

        await api.ConfirmTwoFactorSetupAsync(new VerifyTwoFactorRequest
        {
            Code = code
        }, _ct);

        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        var twoFactorToken = signInResponse.Content!.TwoFactorToken!;

        // Act
        var twoFactorSignInResponse = await api.TwoFactorSignInAsync(new TwoFactorSignInRequest
        {
            TwoFactorToken = twoFactorToken,
            Code = "000000"
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(twoFactorSignInResponse, nameof(TwoFactorSignInRequest.Code),
            "Invalid code.");
    }

    [Fact]
    public async Task DisableTwoFactor_Removes2FaAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var enableResponse = await api.EnableTwoFactorAsync(_ct);
        var secret = enableResponse.Content!.Secret;
        var code = GenerateTotpCode(secret);

        await api.ConfirmTwoFactorSetupAsync(new VerifyTwoFactorRequest
        {
            Code = code
        }, _ct);

        // Act
        var disableResponse = await api.DisableTwoFactorAsync(_ct);

        // Assert
        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorSecret);

        var recoveryCodes = await db.UserRecoveryCodes
            .Where(r => r.UserId == user.Id)
            .CountAsync(_ct);

        Assert.Equal(0, recoveryCodes);
    }

    private static string GenerateTotpCode(string secret)
    {
        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key);
        return totp.ComputeTotp();
    }
}
