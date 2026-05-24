using Peritus.Messaging.Abstractions;

namespace Peritus.Messages.Notification;

public record SendEmailCommand(string To, string Subject, string Body) : ICommand;
