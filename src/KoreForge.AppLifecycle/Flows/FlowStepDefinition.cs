namespace KoreForge.AppLifecycle.Flows;

/// <summary>
/// Immutable description of a step within a flow.
/// </summary>
public sealed class FlowStepDefinition
{
    internal FlowStepDefinition(
        Type stepType,
        IReadOnlyDictionary<FlowOutcome, string> transitions)
    {
        StepType = stepType;
        Transitions = transitions;
    }

    public Type StepType { get; }

    public IReadOnlyDictionary<FlowOutcome, string> Transitions { get; }
}
