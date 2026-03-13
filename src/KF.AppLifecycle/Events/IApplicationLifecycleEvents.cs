namespace KoreForge.AppLifecycle.Events;

/// <summary>
/// Event hub for observing lifecycle flow execution.
/// </summary>
public interface IApplicationLifecycleEvents
{
    event Func<LifecycleFlowsEventArgs, Task>? BeforeStartupFlows;
    event Func<LifecycleFlowsEventArgs, Task>? AfterStartupFlows;

    event Func<LifecycleStepExecutingEventArgs, Task>? StartupStepExecuting;
    event Func<LifecycleStepExecutedEventArgs, Task>? StartupStepExecuted;

    event Func<LifecycleFlowsEventArgs, Task>? BeforeShutdownFlows;
    event Func<LifecycleFlowsEventArgs, Task>? AfterShutdownFlows;

    event Func<LifecycleStepExecutingEventArgs, Task>? ShutdownStepExecuting;
    event Func<LifecycleStepExecutedEventArgs, Task>? ShutdownStepExecuted;
}
