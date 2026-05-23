using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Peritus.Notification.Features;

namespace Peritus.Notification;

public static class NotificationExtensions
{
    public static WebApplicationBuilder AddNotificationModule(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<EmailSendFeature>();
        builder.Services.AddScoped<SmsSendFeature>();

        return builder;
    }
}
