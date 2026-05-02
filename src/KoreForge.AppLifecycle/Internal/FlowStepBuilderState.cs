using KoreForge.AppLifecycle.Flows;

namespace KoreForge.AppLifecycle.Internal;

internal sealed class FlowStepBuilderState
{
    private readonly Dictionary<FlowOutcome, FlowStepBuilderState> _transitions = new();

    public FlowStepBuilderState(Type stepType, string key)
    {
        StepType = stepType;
        Key = key;
    }

    public Type StepType { get; }

    public string Key { get; }

    public IReadOnlyDictionary<FlowOutcome, FlowStepBuilderState> Transitions => _transitions;

    public void AddTransition(FlowOutcome outcome, FlowStepBuilderState next)
    {
        if (_transitions.ContainsKey(outcome))
        {
            throw new ApplicationLifecycleException($"Outcome '{outcome}' already mapped for step '{StepType.FullName}'.");
        }

        _transitions[outcome] = next;
    }
}
