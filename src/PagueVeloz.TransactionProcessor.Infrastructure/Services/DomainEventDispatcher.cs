using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PagueVeloz.TransactionProcessor.Domain.Events;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Services;

public class DomainEventDispatcher : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;
    private readonly Channel<IDomainEvent> _eventChannel;
    private Task? _processingTask;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _eventChannel = Channel.CreateUnbounded<IDomainEvent>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DomainEventDispatcher iniciado");
        _processingTask = ProcessEvents(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DomainEventDispatcher parado");
        _eventChannel.Writer.Complete();
        _cancellationTokenSource.Cancel();
        
        if (_processingTask != null)
        {
            await _processingTask;
        }
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _eventChannel.Writer.WriteAsync(domainEvent, cancellationToken);
    }

    private async Task ProcessEvents(CancellationToken cancellationToken)
    {
        await foreach (var domainEvent in _eventChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Publicar evento via MediatR
                await mediator.Publish(domainEvent, cancellationToken);

                _logger.LogInformation(
                    "Evento de domínio processado: {EventType} em {OccurredOn}",
                    domainEvent.GetType().Name,
                    domainEvent.OccurredOn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento de domínio: {EventType}", domainEvent.GetType().Name);
            }
        }
    }
}

