using Microsoft.Extensions.DependencyInjection;
using Peritus.Messaging.Abstractions;

namespace Peritus.Messaging;

public static class Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMediator()
        {
            services.AddScoped<IMediator, Mediator>();
            return services;
        }

        public IServiceCollection AddCommandHandler<TCommand, TFeature>(
            Func<TFeature, TCommand, CancellationToken, Task> handle)
            where TCommand : class, ICommand
            where TFeature : notnull
        {
            services.AddScoped<ICommandHandler<TCommand>>(sp =>
            {
                var feature = sp.GetRequiredService<TFeature>();
                return new DelegateCommandHandler<TCommand, TFeature>(feature, handle);
            });

            return services;
        }
    }
}
