# Refit client for Identity API

Contracts define a Refit interface used by tests and consumers to call the API with strong types.

## Interface
- `IIdentityApi` declares auth/user endpoints with Refit attributes and typed DTOs:
  ```csharp
  public interface IIdentityApi
  {
      [Get("/users/me")]
      Task<IApiResponse<GetCurrentUserResponse>> GetCurrentUserAsync(CancellationToken ct = default);

      [Post("/auth/signup")]
      Task<IApiResponse<SignUpResponse>> SignUpAsync([Body] SignUpRequest request, CancellationToken ct = default);

      [Post("/auth/signin")]
      Task<IApiResponse<SignInResponse>> SignInAsync([Body] SignInRequest request, CancellationToken ct = default);

      [Post("/auth/refresh")]
      Task<IApiResponse<RefreshTokenResponse>> RefreshTokenAsync([Body] RefreshTokenRequest request, CancellationToken ct = default);

      [Post("/auth/signout")]
      Task<IApiResponse> SignOutAsync([Body] SignOutRequest request, CancellationToken ct = default);
  }
  ```
  @/src/contracts/Peritus.Identity.RestContracts/IIdentityApi.cs#7-32

## Usage in tests
- Integration tests create Refit clients with optional bearer tokens:
  ```csharp
  protected T CreateRestClient<T>(AccessToken? accessToken = null)
  {
      var httpClient = CreateHttpClient();
      if (!string.IsNullOrEmpty(accessToken))
      {
          httpClient.DefaultRequestHeaders.Authorization =
              new AuthenticationHeaderValue("Bearer", accessToken);
      }
      return RestService.For<T>(httpClient);
  }
  ```
  @/tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs#61-72

## Notes
- DTOs live under `Peritus.Identity.RestContracts.*` and use the same strong types as the API/domain.
- Refit clients respect auth by setting `Authorization: Bearer <token>` when provided.
