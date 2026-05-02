using KoreForge.AppLifecycle.Events;
using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Internal;
using KoreForge.AppLifecycle.Options;
using KoreForge.AppLifecycle.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KoreForge.AppLifecycle.Hosting;

internal sealed class ScheduledTasksHostedService : BackgroundService
{
    private static readonly TimeSpan DefaultTriggerFailureDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ApplicationLifecycleOptions _options;
    private readonly ApplicationLifecycleEvents _events;
    private readonly ILogger<ScheduledTasksHostedService> _logger;
    private readonly TimeSpan _triggerFailureDelay;
    private readonly Dictionary<string, SemaphoreSlim> _overlapGuards = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    public ScheduledTasksHostedService(
        IServiceProvider services,
        IHostEnvironment hostEnvironment,
        ApplicationLifecycleOptions options,
        ApplicationLifecycleEvents events,
        ILogger<ScheduledTasksHostedService> logger,
        TimeSpan? triggerFailureDelay = null,
        TimeProvider? timeProvider = null)
    {
        _services = services;
        _hostEnvironment = hostEnvironment;
        _options = options;
        _events = events;
        _logger = logger;
        _triggerFailureDelay = triggerFailureDelay ?? DefaultTriggerFailureDelay;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var flows = _options.BuildScheduledFlows();
        if (flows.Count == 0)
        {
            return;
        }

        var tasks = flows.Select(flow => RunScheduledLoopAsync(flow, stoppingToken)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    internal async Task RunScheduledLoopAsync(ScheduledFlowDefinition definition, CancellationToken stoppingToken)
    {
        var executor = new FlowExecutor<ScheduledContext>();
        SemaphoreSlim? overlapGate = definition.NoOverlap ? GetOrCreateGate(definition.FlowName) : null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await GetDelayAsync(definition, stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var lockTaken = false;

            if (overlapGate != null)
            {
                try
                {
                    lockTaken = await overlapGate.WaitAsync(0, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (!lockTaken)
                {
                    _logger.LogWarning(
                        "Scheduled flow '{Flow}' skipped due to NoOverlap guard.",
                        definition.FlowName);
                    continue;
                }
            }

            var context = new ScheduledContext(
                _services,
                _hostEnvironment,
                definition.FlowName,
                _timeProvider.GetUtcNow());

            try
            {
                await executor.ExecuteAsync(
                        definition.Flow,
                        context,
                        _options.UnmappedOutcomePolicy,
                        _events,
                        section: null,
                        _logger,
                        _options.LogStepExceptions,
                        _options.LogUnmappedOutcomes,
                        stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled flow '{Flow}' threw an exception.", definition.FlowName);
            }
            finally
            {
                if (lockTaken)
                {
                    overlapGate?.Release();
                }
            }
        }
    }

    private async Task<TimeSpan> GetDelayAsync(ScheduledFlowDefinition definition, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var trigger = (IScheduleTrigger)scope.ServiceProvider.GetRequiredService(definition.TriggerType);
            var context = new ScheduledContext(_services, _hostEnvironment, definition.FlowName, _timeProvider.GetUtcNow());
            return await trigger.GetNextDelayAsync(context, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schedule trigger for flow '{Flow}' failed. Using fallback delay.", definition.FlowName);
            return _triggerFailureDelay;
        }
    }

    internal SemaphoreSlim GetOrCreateGate(string flowName)
    {
        lock (_overlapGuards)
        {
            if (!_overlapGuards.TryGetValue(flowName, out var gate))
            {
                gate = new SemaphoreSlim(1, 1);
                _overlapGuards[flowName] = gate;
            }

            return gate;
        }
    }
}
