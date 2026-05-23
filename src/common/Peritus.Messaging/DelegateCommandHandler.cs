using Peritus.Messaging.Abstractions;

namespace Peritus.Messaging;

internal sealed class DelegateCommandHandler<TCommand, TFeature>(
    TFeature feature,
    Func<TFeature, TCommand, CancellationToken, Task> handle)
    : ICommandHandler<TCommand>
    where TCommand : class, ICommand
    where TFeature : notnull
{
    public Task HandleAsync(TCommand command, CancellationToken cancellationToken)
        => handle(feature, command, cancellationToken);
}
