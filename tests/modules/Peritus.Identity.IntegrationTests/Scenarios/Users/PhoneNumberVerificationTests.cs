using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Accounts;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.IntegrationTests.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Scenarios.Users;

public class PhoneNumberVerificationTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SendPhoneNumberVerification_SendsCodeAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var phone = new PhoneNumber($"+1{Random.Shared.Next(100000000, 999999999)}");

        // Act
        var response = await api.SendPhoneNumberVerificationAsync(new PhoneNumberVerificationRequest
        {
            PhoneNumber = phone
        }, _ct);

        // Assert
        Assert.True(response.IsSuccessful);
        Assert.NotNull(TokenCapture.Get(UserTokenType.PhoneNumberVerification));
    }

    [Fact]
    public async Task ConfirmPhoneNumber_WithValidCode_UpdatesPhoneAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var phone = new PhoneNumber($"+1{Random.Shared.Next(100000000, 999999999)}");

        await api.SendPhoneNumberVerificationAsync(new PhoneNumberVerificationRequest
        {
            PhoneNumber = phone
        }, _ct);

        var rawCode = TokenCapture.Get(UserTokenType.PhoneNumberVerification)!;
        Assert.NotNull(rawCode);

        // Act
        var response = await api.ConfirmPhoneNumberAsync(new PhoneNumberVerifyRequest
        {
            Code = rawCode
        }, _ct);

        // Assert
        Assert.True(response.IsSuccessful);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.Equal(phone, user.PhoneNumber);
        Assert.True(user.PhoneNumberConfirmed);
    }

    [Fact]
    public async Task ConfirmPhoneNumber_WithInvalidCode_FailsAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var phone = new PhoneNumber($"+1{Random.Shared.Next(100000000, 999999999)}");

        await api.SendPhoneNumberVerificationAsync(new PhoneNumberVerificationRequest
        {
            PhoneNumber = phone
        }, _ct);

        // Act
        var response = await api.ConfirmPhoneNumberAsync(new PhoneNumberVerifyRequest
        {
            Code = "invalid-code"
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, "Code", "Invalid or expired code.");
    }

    [Fact]
    public async Task SendPhoneNumberVerification_Idempotent_SamePhoneAlreadyConfirmedAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var phone = new PhoneNumber($"+1{Random.Shared.Next(100000000, 999999999)}");

        await api.SendPhoneNumberVerificationAsync(new PhoneNumberVerificationRequest
        {
            PhoneNumber = phone
        }, _ct);

        var rawCode = TokenCapture.Get(UserTokenType.PhoneNumberVerification)!;
        await api.ConfirmPhoneNumberAsync(new PhoneNumberVerifyRequest { Code = rawCode }, _ct);

        // Act
        var response = await api.SendPhoneNumberVerificationAsync(new PhoneNumberVerificationRequest
        {
            PhoneNumber = phone
        }, _ct);

        // Assert - should succeed without sending a new code since phone is already confirmed
        Assert.True(response.IsSuccessful);
    }
}
