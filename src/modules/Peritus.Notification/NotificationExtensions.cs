using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Peritus.Messages.Notification;
using Peritus.Messaging;
using Peritus.Notification.Features;

namespace Peritus.Notification;

public static class NotificationExtensions
{
    public static WebApplicationBuilder AddNotificationModule(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<EmailSendFeature>();
        builder.Services.AddScoped<SmsSendFeature>();

        builder.Services.AddCommandHandler<SendEmailCommand, EmailSendFeature>((feature, command, _) =>
        {
            feature.Execute(new EmailSendFeature.Context
            {
                To = command.To,
                Subject = command.Subject,
                Body = command.Body
            });

            return Task.CompletedTask;
        });

        builder.Services.AddCommandHandler<SendSmsCommand, SmsSendFeature>((feature, command, _) =>
        {
            feature.Execute(new SmsSendFeature.Context
            {
                To = command.To,
                Message = command.Message
            });

            return Task.CompletedTask;
        });

        return builder;
    }
}
