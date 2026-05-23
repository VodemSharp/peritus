using Microsoft.Extensions.DependencyInjection;
using Peritus.Messaging.Abstractions;

namespace Peritus.Messaging;

public class Mediator(IServiceProvider services) : IMediator
{
    public Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : class, ICommand
    {
        var handler = services.GetRequiredService<ICommandHandler<TCommand>>();
        return handler.HandleAsync(command, cancellationToken);
    }
}
