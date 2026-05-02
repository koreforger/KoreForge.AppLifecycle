using KoreForge.AppLifecycle.Flows;

namespace KoreForge.AppLifecycle.Events;

/// <summary>
/// Event args raised after a flow step completes.
/// </summary>
public sealed class LifecycleStepExecutedEventArgs : EventArgs
{
    public LifecycleStepExecutedEventArgs(
        IServiceProvider services,
        LifecycleSection section,
        string flowName,
        Type stepType,
        FlowOutcome outcome,
        TimeSpan duration,
        Exception? exception)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Section = section;
        FlowName = flowName ?? throw new ArgumentNullException(nameof(flowName));
        StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
        Outcome = outcome;
        Duration = duration;
        Exception = exception;
    }

    public IServiceProvider Services { get; }

    public LifecycleSection Section { get; }

    public string FlowName { get; }

    public Type StepType { get; }

    public FlowOutcome Outcome { get; }

    public TimeSpan Duration { get; }

    public Exception? Exception { get; }
}
