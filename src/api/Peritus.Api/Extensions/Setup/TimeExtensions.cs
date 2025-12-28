namespace Peritus.Api.Extensions.Setup;

public static class TimeExtensions
{
    public static void ConfigureTime(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);
    }
}
