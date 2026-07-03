namespace Peritus.FluentResults;

public static class ErrorCodes
{
    public static readonly ErrorCode Validation = new("VALIDATION_ERROR", "One or more validation errors occurred.");
    public static readonly ErrorCode Internal = new("INTERNAL_ERROR", "An unexpected error occurred.");
}
