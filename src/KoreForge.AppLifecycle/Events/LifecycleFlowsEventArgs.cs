namespace KoreForge.AppLifecycle.Events;

/// <summary>
/// Event args emitted before and after a series of flows runs.
/// </summary>
public sealed class LifecycleFlowsEventArgs : EventArgs
{
    public LifecycleFlowsEventArgs(IServiceProvider services, LifecycleSection section)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Section = section;
    }

    public IServiceProvider Services { get; }

    public LifecycleSection Section { get; }
}
