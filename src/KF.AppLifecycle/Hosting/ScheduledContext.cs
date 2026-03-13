using Microsoft.Extensions.Hosting;

namespace KoreForge.AppLifecycle.Hosting;

/// <summary>
/// Context passed to scheduled flow steps and triggers.
/// </summary>
public sealed class ScheduledContext : IFlowContext
{
    public ScheduledContext(
        IServiceProvider services,
        IHostEnvironment hostEnvironment,
        string flowName,
        DateTimeOffset scheduledTime)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        FlowName = flowName ?? throw new ArgumentNullException(nameof(flowName));
        ScheduledTime = scheduledTime;
    }

    public IServiceProvider Services { get; }

    public IHostEnvironment HostEnvironment { get; }

    public DateTimeOffset ScheduledTime { get; }

    public string FlowName { get; }
}
