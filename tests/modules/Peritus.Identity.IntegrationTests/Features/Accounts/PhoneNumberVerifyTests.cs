using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Accounts;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Helpers;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Features.Accounts;

public class PhoneNumberVerifyTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ConfirmPhoneNumber_WithValidCode_UpdatesPhoneAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);

        var phone = PhoneNumberTestHelper.Generate();

        await api.SendPhoneNumberVerificationAsync(new PhoneNumberSendVerificationRequest
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
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);

        var phone = PhoneNumberTestHelper.Generate();

        await api.SendPhoneNumberVerificationAsync(new PhoneNumberSendVerificationRequest
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
}
