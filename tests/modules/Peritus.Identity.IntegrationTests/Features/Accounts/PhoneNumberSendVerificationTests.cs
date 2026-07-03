using Peritus.ApiContracts.Identity.Accounts;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Helpers;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Features.Accounts;

public class PhoneNumberSendVerificationTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SendPhoneNumberVerification_SendsCodeAsync()
    {
        // Arrange
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);

        var phone = PhoneNumberTestHelper.Generate();

        // Act
        var response = await api.SendPhoneNumberVerificationAsync(new PhoneNumberSendVerificationRequest
        {
            PhoneNumber = phone
        }, _ct);

        // Assert
        Assert.True(response.IsSuccessful);
        Assert.NotNull(TokenCapture.Get(UserTokenType.PhoneNumberVerification));
    }

    [Fact]
    public async Task SendPhoneNumberVerification_Idempotent_SamePhoneAlreadyConfirmedAsync()
    {
        // Arrange
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);

        var phone = PhoneNumberTestHelper.Generate();

        await api.SendPhoneNumberVerificationAsync(new PhoneNumberSendVerificationRequest
        {
            PhoneNumber = phone
        }, _ct);

        var rawCode = TokenCapture.Get(UserTokenType.PhoneNumberVerification)!;
        await api.ConfirmPhoneNumberAsync(new PhoneNumberVerifyRequest
        {
            Code = rawCode
        }, _ct);

        // Act
        var response = await api.SendPhoneNumberVerificationAsync(new PhoneNumberSendVerificationRequest
        {
            PhoneNumber = phone
        }, _ct);

        // Assert
        Assert.True(response.IsSuccessful);
    }
}
