using Peritus.FluentResults;

namespace Peritus.Identity.Types;

public static class IdentityErrorCodes
{
    // Credentials & lockout
    public static readonly ErrorCode InvalidCredentials =
        new("INVALID_CREDENTIALS", "Invalid credentials.");

    public static readonly ErrorCode AccountLocked =
        new("ACCOUNT_LOCKED", "Account locked due to multiple failed attempts. Try again in {0} minutes.");

    public static readonly ErrorCode CurrentPasswordIncorrect =
        new("CURRENT_PASSWORD_INCORRECT", "Current password is incorrect.");

    // Email
    public static readonly ErrorCode EmailNotConfirmed =
        new("EMAIL_NOT_CONFIRMED", "Email not confirmed.");

    public static readonly ErrorCode EmailAlreadyInUse =
        new("EMAIL_ALREADY_IN_USE", "Email address is already in use.");

    // Tokens
    public static readonly ErrorCode InvalidAccessToken =
        new("INVALID_ACCESS_TOKEN", "Invalid access token.");

    public static readonly ErrorCode InvalidEmailToken =
        new("INVALID_EMAIL_TOKEN", "Invalid email confirmation token.");

    public static readonly ErrorCode EmailTokenExpired =
        new("EMAIL_TOKEN_EXPIRED", "Email confirmation token has expired.");

    public static readonly ErrorCode InvalidPasswordResetToken =
        new("INVALID_PASSWORD_RESET_TOKEN", "Invalid password reset token.");

    public static readonly ErrorCode PasswordResetTokenExpired =
        new("PASSWORD_RESET_TOKEN_EXPIRED", "Password reset token has expired.");

    public static readonly ErrorCode InvalidGoogleToken =
        new("INVALID_GOOGLE_TOKEN", "Invalid or unverified Google token.");

    // Sessions
    public static readonly ErrorCode SessionNotFound =
        new("SESSION_NOT_FOUND", "Session not found.");

    public static readonly ErrorCode SessionExpired =
        new("SESSION_EXPIRED", "User session has expired.");

    public static readonly ErrorCode SessionTerminated =
        new("SESSION_TERMINATED", "User session has been terminated.");

    public static readonly ErrorCode RefreshTokenNotFound =
        new("REFRESH_TOKEN_NOT_FOUND", "Refresh token not found.");

    // Two-factor
    public static readonly ErrorCode TwoFactorNotEnabled =
        new("TWO_FACTOR_NOT_ENABLED", "Two-factor authentication is not enabled.");

    public static readonly ErrorCode TwoFactorNotInitiated =
        new("TWO_FACTOR_NOT_INITIATED", "Two-factor authentication setup has not been initiated.");

    public static readonly ErrorCode InvalidTwoFactorToken =
        new("INVALID_TWO_FACTOR_TOKEN", "Invalid two-factor token.");

    public static readonly ErrorCode InvalidTwoFactorSetupCode =
        new("INVALID_TWO_FACTOR_SETUP_CODE", "Invalid two-factor setup code.");

    public static readonly ErrorCode InvalidTwoFactorCode =
        new("INVALID_TWO_FACTOR_CODE", "Invalid two-factor code.");

    public static readonly ErrorCode InvalidRecoveryCode =
        new("INVALID_RECOVERY_CODE", "Invalid recovery code.");

    // Phone
    public static readonly ErrorCode InvalidPhoneNumber =
        new("INVALID_PHONE_NUMBER", "Invalid phone number format.");

    public static readonly ErrorCode InvalidOrExpiredPhoneCode =
        new("INVALID_OR_EXPIRED_PHONE_CODE", "Invalid or expired code.");
}
