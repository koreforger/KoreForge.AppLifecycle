namespace KoreForge.AppLifecycle.Events;

/// <summary>
/// Event args raised immediately before a flow step executes.
/// </summary>
public sealed class LifecycleStepExecutingEventArgs : EventArgs
{
    public LifecycleStepExecutingEventArgs(
        IServiceProvider services,
        LifecycleSection section,
        string flowName,
        Type stepType)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Section = section;
        FlowName = flowName ?? throw new ArgumentNullException(nameof(flowName));
        StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
    }

    public IServiceProvider Services { get; }

    public LifecycleSection Section { get; }

    public string FlowName { get; }

    public Type StepType { get; }
}
