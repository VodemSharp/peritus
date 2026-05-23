namespace Peritus.Messaging.Abstractions;

public interface IMediator
{
    Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : class, ICommand;
}
