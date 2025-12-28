using Peritus.OpenApi.Transformers;
using Scalar.AspNetCore;

namespace Peritus.Api.Extensions.Setup;

public static class OpenApiExtensions
{
    public static WebApplicationBuilder ConfigureOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();
        });

        return builder;
    }

    public static WebApplication MapOpenApiWithScalar(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();

        return app;
    }
}
