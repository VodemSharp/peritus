using Peritus.ApiContracts.Identity.Accounts;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.ApiContracts.Identity.Profile;
using Refit;

namespace Peritus.ApiContracts.Identity;

public interface IIdentityApi
{
    #region Auth

    [Post("/auth/signup")]
    Task<IApiResponse<SignUpResponse>> SignUpAsync([Body] SignUpRequest request, CancellationToken ct = default);

    [Post("/auth/signin")]
    Task<IApiResponse<SignInResponse>> SignInAsync([Body] SignInRequest request, CancellationToken ct = default);

    [Post("/auth/verify-2fa")]
    Task<IApiResponse<TwoFactorSignInResponse>> TwoFactorSignInAsync([Body] TwoFactorSignInRequest request,
        CancellationToken ct = default);

    [Post("/auth/use-recovery-code")]
    Task<IApiResponse<RecoveryCodeSignInResponse>> RecoveryCodeSignInAsync([Body] RecoveryCodeSignInRequest request,
        CancellationToken ct = default);

    [Post("/auth/external/google")]
    Task<IApiResponse<GoogleSignInResponse>> GoogleSignInAsync([Body] GoogleSignInRequest request,
        CancellationToken ct = default);

    [Post("/auth/signout")]
    Task<IApiResponse> SignOutAsync(CancellationToken ct = default);

    [Post("/auth/refresh")]
    Task<IApiResponse<RefreshTokenResponse>> RefreshTokenAsync([Body] RefreshTokenRequest request,
        CancellationToken ct = default);

    [Post("/auth/emails/send-confirmation")]
    Task<IApiResponse> SendEmailConfirmationAsync([Body] EmailConfirmationRequest request,
        CancellationToken ct = default);

    [Post("/auth/emails/confirm")]
    Task<IApiResponse> ConfirmEmailAsync([Body] ConfirmEmailRequest request, CancellationToken ct = default);

    [Post("/auth/passwords/forgot")]
    Task<IApiResponse> SendPasswordResetAsync([Body] SendPasswordResetRequest request, CancellationToken ct = default);

    [Post("/auth/passwords/reset")]
    Task<IApiResponse> ResetPasswordAsync([Body] ResetPasswordRequest request, CancellationToken ct = default);

    #endregion

    #region Accounts

    [Post("/accounts/2fa/enable")]
    Task<IApiResponse<EnableTwoFactorResponse>> EnableTwoFactorAsync(CancellationToken ct = default);

    [Post("/accounts/2fa/confirm")]
    Task<IApiResponse<VerifyTwoFactorResponse>> ConfirmTwoFactorSetupAsync([Body] VerifyTwoFactorRequest request,
        CancellationToken ct = default);

    [Post("/accounts/2fa/disable")]
    Task<IApiResponse> DisableTwoFactorAsync(CancellationToken ct = default);

    [Post("/accounts/2fa/recovery-codes")]
    Task<IApiResponse<VerifyTwoFactorResponse>> GenerateRecoveryCodesAsync(CancellationToken ct = default);

    [Post("/accounts/passwords/change")]
    Task<IApiResponse> ChangePasswordAsync([Body] ChangePasswordRequest request, CancellationToken ct = default);

    [Get("/accounts/sessions")]
    Task<IApiResponse<List<SessionResponse>>> GetUserSessionsAsync(CancellationToken ct = default);

    [Post("/accounts/sessions/{id}/revoke")]
    Task<IApiResponse> RevokeUserSessionAsync(string id, CancellationToken ct = default);

    [Post("/accounts/sessions/revoke-all")]
    Task<IApiResponse> RevokeAllSessionsAsync(CancellationToken ct = default);

    [Post("/accounts/phones/send-verification")]
    Task<IApiResponse> SendPhoneNumberVerificationAsync([Body] PhoneNumberVerificationRequest request,
        CancellationToken ct = default);

    [Post("/accounts/phones/verify")]
    Task<IApiResponse> ConfirmPhoneNumberAsync([Body] PhoneNumberVerifyRequest request, CancellationToken ct = default);

    #endregion

    #region Profile

    [Get("/profiles")]
    Task<IApiResponse<GetCurrentUserResponse>> GetCurrentUserAsync(CancellationToken ct = default);

    [Put("/profiles")]
    Task<IApiResponse> UpdateProfileAsync([Body] UpdateProfileRequest request, CancellationToken ct = default);

    #endregion
}
