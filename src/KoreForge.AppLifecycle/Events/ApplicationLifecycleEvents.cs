using KoreForge.AppLifecycle.Options;

namespace KoreForge.AppLifecycle.Events;

internal sealed class ApplicationLifecycleEvents : IApplicationLifecycleEvents
{
    public ApplicationLifecycleEvents(ApplicationLifecycleOptions options)
    {
        foreach (var registration in options.Events.Registrations)
        {
            registration(this);
        }
    }

    public event Func<LifecycleFlowsEventArgs, Task>? BeforeStartupFlows;
    public event Func<LifecycleFlowsEventArgs, Task>? AfterStartupFlows;
    public event Func<LifecycleStepExecutingEventArgs, Task>? StartupStepExecuting;
    public event Func<LifecycleStepExecutedEventArgs, Task>? StartupStepExecuted;
    public event Func<LifecycleFlowsEventArgs, Task>? BeforeShutdownFlows;
    public event Func<LifecycleFlowsEventArgs, Task>? AfterShutdownFlows;
    public event Func<LifecycleStepExecutingEventArgs, Task>? ShutdownStepExecuting;
    public event Func<LifecycleStepExecutedEventArgs, Task>? ShutdownStepExecuted;

    internal Func<LifecycleFlowsEventArgs, Task>? GetFlowsHandler(LifecycleSection section, bool before)
    {
        return section switch
        {
            LifecycleSection.Startup => before ? BeforeStartupFlows : AfterStartupFlows,
            LifecycleSection.Shutdown => before ? BeforeShutdownFlows : AfterShutdownFlows,
            _ => null
        };
    }

    internal (Func<LifecycleStepExecutingEventArgs, Task>?, Func<LifecycleStepExecutedEventArgs, Task>?) GetStepHandlers(LifecycleSection section)
    {
        return section switch
        {
            LifecycleSection.Startup => (StartupStepExecuting, StartupStepExecuted),
            LifecycleSection.Shutdown => (ShutdownStepExecuting, ShutdownStepExecuted),
            _ => (null, null)
        };
    }
}
