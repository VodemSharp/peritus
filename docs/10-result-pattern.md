# Result pattern (exceptions replaced with FluentResult)

Application features return `FluentResult`/`FluentResult<T>` to convey success/failure without exceptions.

## Core API
- `FluentResult` encapsulates success flag and optional error payloads:
  ```csharp
  public class FluentResult
  {
      protected FluentResult(bool isSuccess, IFluentResultError? error = null)
      {
          IsSuccess = isSuccess;
          Error = error;
      }

      public bool IsSuccess { get; }
      public IFluentResultError? Error { get; }

      public static FluentResult Success() => new(true);
      public static FluentResult ValidationProblem(string field, string message) =>
          new(false, new FluentValidationProblemResult { Errors = new Dictionary<string, string[]> { { field, [message] } } });
      public static FluentResult ValidationProblem(Dictionary<string, string[]> errors) =>
          new(false, new FluentValidationProblemResult { Errors = errors });
      public static FluentResult NotFound(string? detail = null) => new(false, new FluentNotFoundResult { Detail = detail });
      public static FluentResult InternalError(string? detail = null) => new(false, new FluentInternalErrorResult { Detail = detail });
  }
  ```
  @/src/common/Peritus.Results/FluentResult.cs#3-56

- Generic variant carries a result payload:
  ```csharp
  public sealed class FluentResult<TResult> : FluentResult
  {
      private FluentResult(bool isSuccess, TResult? result, IFluentResultError? error = null) : base(isSuccess, error)
      {
          Result = result;
      }

      public TResult? Result { get; }

      public static FluentResult<TResult> Success(TResult result) => new(true, result);
      public static new FluentResult<TResult> ValidationProblem(string field, string message) =>
          new(false, default, new FluentValidationProblemResult { Errors = new Dictionary<string, string[]> { { field, [message] } } });
      public static new FluentResult<TResult> ValidationProblem(Dictionary<string, string[]> errors) =>
          new(false, default, new FluentValidationProblemResult { Errors = errors });
      public static new FluentResult<TResult> NotFound(string? detail = null) => new(false, default, new FluentNotFoundResult { Detail = detail });
      public static new FluentResult<TResult> InternalError(string? detail = null) => new(false, default, new FluentInternalErrorResult { Detail = detail });
  }
  ```
  @/src/common/Peritus.Results/FluentResult.cs#58-108

## Usage pattern
- Features/services return `FluentResult<T>` instead of throwing, enabling API layer to map to HTTP codes (validation → 400, not found → 404, internal error → 500) as described in solution docs.

## Extending
- Add new factory helpers in `FluentResult` for additional error shapes.
- Implement `IFluentResultError` subtypes to model domain-specific errors without exceptions.
