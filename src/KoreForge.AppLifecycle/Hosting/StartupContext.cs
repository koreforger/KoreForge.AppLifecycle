using Microsoft.Extensions.Hosting;

namespace KoreForge.AppLifecycle.Hosting;

/// <summary>
/// Context passed to startup flow steps.
/// </summary>
public sealed class StartupContext : IFlowContext
{
    public StartupContext(IServiceProvider services, IHostEnvironment hostEnvironment)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    }

    public IServiceProvider Services { get; }

    public IHostEnvironment HostEnvironment { get; }
}
