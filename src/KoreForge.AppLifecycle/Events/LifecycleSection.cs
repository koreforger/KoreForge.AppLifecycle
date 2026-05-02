namespace KoreForge.AppLifecycle.Events;

/// <summary>
/// Identifies whether a flow is part of startup or shutdown.
/// </summary>
public enum LifecycleSection
{
    Startup = 0,
    Shutdown = 1
}
