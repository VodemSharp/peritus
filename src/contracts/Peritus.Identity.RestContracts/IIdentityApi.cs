using Peritus.Identity.RestContracts.Auth;
using Peritus.Identity.RestContracts.Users;
using Refit;

namespace Peritus.Identity.RestContracts;

public interface IIdentityApi
{
    #region Users

    [Get("/users/me")]
    Task<IApiResponse<GetCurrentUserResponse>> GetCurrentUserAsync(CancellationToken ct = default);

    #endregion

    #region Auth

    [Post("/auth/signup")]
    Task<IApiResponse<SignUpResponse>> SignUpAsync([Body] SignUpRequest request, CancellationToken ct = default);

    [Post("/auth/signin")]
    Task<IApiResponse<SignInResponse>> SignInAsync([Body] SignInRequest request, CancellationToken ct = default);

    [Post("/auth/refresh")]
    Task<IApiResponse<RefreshTokenResponse>> RefreshTokenAsync([Body] RefreshTokenRequest request,
        CancellationToken ct = default);

    [Post("/auth/signout")]
    Task<IApiResponse> SignOutAsync([Body] SignOutRequest request, CancellationToken ct = default);

    #endregion
}
