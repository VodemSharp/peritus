using Microsoft.Extensions.Logging;

namespace Peritus.Notification.Features;

public partial class EmailSendFeature(ILogger<EmailSendFeature> logger)
{
    public void Execute(Context context)
    {
        LogEmailSend(context.To, context.Subject, context.Body);
    }

    [LoggerMessage(LogLevel.Information, "[DEV] Email to {To}\n  Subject: {Subject}\n  Body: {Body}")]
    private partial void LogEmailSend(string to, string subject, string body);

    public class Context
    {
        public required string To { get; set; }
        public required string Subject { get; set; }
        public required string Body { get; set; }
    }
}
