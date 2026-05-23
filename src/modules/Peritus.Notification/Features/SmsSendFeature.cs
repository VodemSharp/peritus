using Microsoft.Extensions.Logging;

namespace Peritus.Notification.Features;

public partial class SmsSendFeature(ILogger<SmsSendFeature> logger)
{
    public void Execute(Context context)
    {
        LogSmsSend(context.To, context.Message);
    }

    [LoggerMessage(LogLevel.Information, "[DEV] SMS to {To}\n  Message: {Message}")]
    private partial void LogSmsSend(string to, string message);

    public class Context
    {
        public required string To { get; set; }
        public required string Message { get; set; }
    }
}
