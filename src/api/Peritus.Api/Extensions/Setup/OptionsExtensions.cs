using Microsoft.AspNetCore.HttpOverrides;

namespace Peritus.Api.Extensions.Setup;

public static class OptionsExtensions
{
    public static WebApplicationBuilder ConfigureOptions(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        return builder;
    }
}
