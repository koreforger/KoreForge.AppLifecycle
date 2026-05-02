using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Options;

/// <summary>
/// Configures startup, shutdown, and scheduled flows along with behavior flags.
/// </summary>
public sealed class ApplicationLifecycleOptions
{
    private readonly StartupFlowConfigurator _startup;
    private readonly ShutdownFlowConfigurator _shutdown;
    private readonly ScheduledFlowConfigurator _scheduled;

    public ApplicationLifecycleOptions()
    {
        _startup = new StartupFlowConfigurator();
        _shutdown = new ShutdownFlowConfigurator();
        _scheduled = new ScheduledFlowConfigurator();
        Events = new LifecycleEventsConfigurator();
    }

    /// <summary>
    /// Gets the startup flow configurator.
    /// </summary>
    public StartupFlowConfigurator Startup => _startup;

    /// <summary>
    /// Gets the shutdown flow configurator.
    /// </summary>
    public ShutdownFlowConfigurator Shutdown => _shutdown;

    /// <summary>
    /// Gets the scheduled flow configurator.
    /// </summary>
    public ScheduledFlowConfigurator Scheduled => _scheduled;

    /// <summary>
    /// Gets the lifecycle events configurator.
    /// </summary>
    public LifecycleEventsConfigurator Events { get; }

    /// <summary>
    /// Gets or sets a value indicating whether startup failures abort host startup.
    /// </summary>
    public bool FailFastOnStartupFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether shutdown failures should throw.
    /// </summary>
    public bool FailFastOnShutdownFailure { get; set; } = false;

    /// <summary>
    /// Gets or sets the policy for steps that return unmapped outcomes.
    /// </summary>
    public UnmappedOutcomePolicy UnmappedOutcomePolicy { get; set; } = UnmappedOutcomePolicy.StopFlow;

    /// <summary>
    /// Gets or sets a value indicating whether step exceptions are logged.
    /// </summary>
    public bool LogStepExceptions { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether unmapped outcomes are logged when tolerated.
    /// </summary>
    public bool LogUnmappedOutcomes { get; set; } = false;

    internal IReadOnlyList<FlowDefinition<StartupContext>> BuildStartupFlows() => _startup.BuildDefinitions();

    internal IReadOnlyList<FlowDefinition<ShutdownContext>> BuildShutdownFlows() => _shutdown.BuildDefinitions();

    internal IReadOnlyList<Scheduling.ScheduledFlowDefinition> BuildScheduledFlows() => _scheduled.BuildDefinitions();
}
