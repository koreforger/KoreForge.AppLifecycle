using Microsoft.Extensions.Hosting;

namespace KoreForge.AppLifecycle.Hosting;

/// <summary>
/// Context passed to shutdown flow steps.
/// </summary>
public sealed class ShutdownContext : IFlowContext
{
    public ShutdownContext(IServiceProvider services, IHostEnvironment hostEnvironment)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    }

    public IServiceProvider Services { get; }

    public IHostEnvironment HostEnvironment { get; }
}
