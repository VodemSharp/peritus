using Peritus.Messaging;
using Peritus.Messaging.Abstractions;

namespace Peritus.Messages.Notification;

public record SendSmsCommand(string To, string Message) : ICommand;
