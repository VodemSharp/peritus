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
    Task<IApiResponse<SignInTwoFactorResponse>> TwoFactorSignInAsync([Body] SignInTwoFactorRequest request,
        CancellationToken ct = default);

    [Post("/auth/use-recovery-code")]
    Task<IApiResponse<SignInRecoveryCodeResponse>> RecoveryCodeSignInAsync([Body] SignInRecoveryCodeRequest request,
        CancellationToken ct = default);

    [Post("/auth/signin/google")]
    Task<IApiResponse<SignInGoogleResponse>> GoogleSignInAsync([Body] SignInGoogleRequest request,
        CancellationToken ct = default);

    [Post("/auth/refresh")]
    Task<IApiResponse<TokenRefreshResponse>> RefreshTokenAsync([Body] TokenRefreshRequest request,
        CancellationToken ct = default);

    [Post("/auth/emails/send-confirmation")]
    Task<IApiResponse> SendEmailConfirmationAsync([Body] EmailSendConfirmationRequest request,
        CancellationToken ct = default);

    [Post("/auth/emails/confirm")]
    Task<IApiResponse> ConfirmEmailAsync([Body] EmailConfirmRequest request, CancellationToken ct = default);

    [Post("/auth/passwords/forgot")]
    Task<IApiResponse> SendPasswordResetAsync([Body] PasswordResetSendRequest request, CancellationToken ct = default);

    [Post("/auth/passwords/reset")]
    Task<IApiResponse> ResetPasswordAsync([Body] PasswordResetRequest request, CancellationToken ct = default);

    #endregion

    #region Accounts

    [Post("/accounts/signout")]
    Task<IApiResponse> SignOutAsync(CancellationToken ct = default);

    [Post("/accounts/2fa/enable")]
    Task<IApiResponse<TwoFactorEnableResponse>> EnableTwoFactorAsync(CancellationToken ct = default);

    [Post("/accounts/2fa/confirm")]
    Task<IApiResponse<TwoFactorVerifySetupResponse>> ConfirmTwoFactorSetupAsync(
        [Body] TwoFactorVerifySetupRequest request,
        CancellationToken ct = default);

    [Post("/accounts/2fa/disable")]
    Task<IApiResponse> DisableTwoFactorAsync(CancellationToken ct = default);

    [Post("/accounts/2fa/recovery-codes")]
    Task<IApiResponse<TwoFactorRecoveryCodesResponse>> GenerateRecoveryCodesAsync(CancellationToken ct = default);

    [Post("/accounts/passwords/change")]
    Task<IApiResponse> ChangePasswordAsync([Body] PasswordChangeRequest request, CancellationToken ct = default);

    [Get("/accounts/sessions")]
    Task<IApiResponse<List<SessionListItemResponse>>> GetUserSessionsAsync(CancellationToken ct = default);

    [Post("/accounts/sessions/{id}/revoke")]
    Task<IApiResponse> RevokeUserSessionAsync(string id, CancellationToken ct = default);

    [Post("/accounts/sessions/revoke-all")]
    Task<IApiResponse> RevokeAllSessionsAsync(CancellationToken ct = default);

    [Post("/accounts/phones/send-verification")]
    Task<IApiResponse> SendPhoneNumberVerificationAsync([Body] PhoneNumberSendVerificationRequest request,
        CancellationToken ct = default);

    [Post("/accounts/phones/verify")]
    Task<IApiResponse> ConfirmPhoneNumberAsync([Body] PhoneNumberVerifyRequest request, CancellationToken ct = default);

    #endregion

    #region Profile

    [Get("/profiles")]
    Task<IApiResponse<ProfileGetResponse>> GetCurrentUserAsync(CancellationToken ct = default);

    [Put("/profiles")]
    Task<IApiResponse> UpdateProfileAsync([Body] ProfileUpdateRequest request, CancellationToken ct = default);

    #endregion
}
