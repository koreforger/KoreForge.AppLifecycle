namespace KoreForge.AppLifecycle.Hosting;

/// <summary>
/// Marker interface for flow execution contexts.
/// </summary>
public interface IFlowContext
{
    IServiceProvider Services { get; }
}
