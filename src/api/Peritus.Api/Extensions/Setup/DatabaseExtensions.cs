using Dapper;
using Peritus.Persistence.Extensions;

namespace Peritus.Api.Extensions.Setup;

public static class DatabaseExtensions
{
    public static WebApplicationBuilder ConfigureDatabase(this WebApplicationBuilder builder)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        builder.Services.AddInterceptors();

        return builder;
    }
}
