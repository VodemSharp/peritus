using Peritus.Api.Accessors;

namespace Peritus.Api.Extensions.Setup;

public static class AccessorExtensions
{
    public static WebApplicationBuilder ConfigureAccessors(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<CultureAccessor>();

        return builder;
    }
}
