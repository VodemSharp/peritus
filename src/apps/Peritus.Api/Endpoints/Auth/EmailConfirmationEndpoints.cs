using Peritus.Api.Extensions;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.Features.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class EmailConfirmationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/auth/emails")
            .WithTags("Emails");

        group.MapPost("/confirm", HandleConfirmEmailAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Confirm email");

        group.MapPost("/send-confirmation", HandleSendEmailConfirmationAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Send email confirmation");
    }

    private static async Task<IResult> HandleConfirmEmailAsync(
        ConfirmEmailRequest request,
        EmailConfirmFeature feature,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new EmailConfirmFeature.Context
            {
                Email = request.Email,
                Token = request.Token
            }, ct);

        return result.ToResult();
    }

    private static async Task<IResult> HandleSendEmailConfirmationAsync(
        EmailConfirmationRequest request,
        EmailSendConfirmationFeature feature,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new EmailSendConfirmationFeature.Context
            {
                Email = request.Email
            }, ct);

        return result.ToResult();
    }
}
