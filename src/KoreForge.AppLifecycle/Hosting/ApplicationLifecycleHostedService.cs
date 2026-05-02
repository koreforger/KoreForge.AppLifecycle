using KoreForge.AppLifecycle.Events;
using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Internal;
using KoreForge.AppLifecycle.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KoreForge.AppLifecycle.Hosting;

internal sealed class ApplicationLifecycleHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ApplicationLifecycleOptions _options;
    private readonly ApplicationLifecycleEvents _events;
    private readonly ILogger<ApplicationLifecycleHostedService> _logger;

    public ApplicationLifecycleHostedService(
        IServiceProvider services,
        IHostEnvironment hostEnvironment,
        ApplicationLifecycleOptions options,
        ApplicationLifecycleEvents events,
        ILogger<ApplicationLifecycleHostedService> logger)
    {
        _services = services;
        _hostEnvironment = hostEnvironment;
        _options = options;
        _events = events;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var flows = _options.BuildStartupFlows();
        if (flows.Count == 0)
        {
            return;
        }

        var context = new StartupContext(_services, _hostEnvironment);
        await RunFlowsAsync(flows, context, LifecycleSection.Startup, _options.FailFastOnStartupFailure, cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var flows = _options.BuildShutdownFlows();
        if (flows.Count == 0)
        {
            return;
        }

        var context = new ShutdownContext(_services, _hostEnvironment);
        await RunFlowsAsync(flows, context, LifecycleSection.Shutdown, _options.FailFastOnShutdownFailure, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunFlowsAsync<TContext>(
        IReadOnlyList<FlowDefinition<TContext>> flows,
        TContext context,
        LifecycleSection section,
        bool failFast,
        CancellationToken cancellationToken)
        where TContext : IFlowContext
    {
        var executor = new FlowExecutor<TContext>();
        var failures = new List<Exception>();
        await RaiseFlowsEventAsync(section, before: true, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Starting {Section} flows ({Count}).", section, flows.Count);

        foreach (var flow in flows)
        {
            try
            {
                var outcome = await executor.ExecuteAsync(
                        flow,
                        context,
                        _options.UnmappedOutcomePolicy,
                        _events,
                        section,
                        _logger,
                        _options.LogStepExceptions,
                        _options.LogUnmappedOutcomes,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (outcome == FlowOutcome.Failure)
                {
                    var failure = new ApplicationLifecycleException($"Flow '{flow.Name}' completed with Failure outcome.");
                    failures.Add(failure);
                    _logger.LogWarning("Flow '{Flow}' completed with Failure outcome.", flow.Name);

                    if (failFast)
                    {
                        throw failure;
                    }
                }
            }
            catch (Exception ex) when (!failFast)
            {
                failures.Add(ex);
                _logger.LogError(ex, "Flow '{Flow}' threw an exception during {Section} stage.", flow.Name, section);
            }
            catch (Exception ex)
            {
                throw new ApplicationLifecycleException($"Flow '{flow.Name}' failed during {section} stage.", ex);
            }
        }

        _logger.LogInformation("Completed {Section} flows.", section);
        await RaiseFlowsEventAsync(section, before: false, cancellationToken).ConfigureAwait(false);

        if (failures.Count > 0 && !failFast)
        {
            _logger.LogWarning("{Count} {Section} flow(s) reported failures.", failures.Count, section);
        }
    }

    private async Task RaiseFlowsEventAsync(LifecycleSection section, bool before, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var handler = _events.GetFlowsHandler(section, before);
        var args = new LifecycleFlowsEventArgs(_services, section);
        var name = before ? $"Before{section}Flows" : $"After{section}Flows";
        await EventDispatcher.InvokeAsync(handler, args, _logger, name).ConfigureAwait(false);
    }
}
