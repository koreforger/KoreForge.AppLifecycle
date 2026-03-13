using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Internal;

internal sealed class FlowBuilderState<TContext> where TContext : IFlowContext
{
    private readonly string _flowName;
    private readonly Dictionary<Type, FlowStepBuilderState> _stepsByType = new();
    private readonly Dictionary<string, FlowStepBuilderState> _stepsByKey = new();

    public FlowBuilderState(string flowName)
    {
        _flowName = flowName;
    }

    private string? _startStepKey;

    public FlowStepBuilderState BeginWith(Type stepType)
    {
        if (_startStepKey != null)
        {
            throw new ApplicationLifecycleException($"Flow '{_flowName}' already has a starting step.");
        }

        var step = GetOrCreateStep(stepType);
        _startStepKey = step.Key;
        return step;
    }

    public FlowStepBuilderState GetOrCreateStep(Type stepType)
    {
        if (!_stepsByType.TryGetValue(stepType, out var step))
        {
            var key = stepType.FullName ?? stepType.Name;
            step = new FlowStepBuilderState(stepType, key);
            _stepsByType[stepType] = step;
            _stepsByKey[key] = step;
        }

        return step;
    }

    public void AddTransition(FlowStepBuilderState source, FlowOutcome outcome, FlowStepBuilderState target)
    {
        source.AddTransition(outcome, target);
    }

    public FlowDefinition<TContext> Build()
    {
        if (_startStepKey is null)
        {
            throw new ApplicationLifecycleException($"Flow '{_flowName}' must define a starting step.");
        }

        var steps = new Dictionary<string, FlowStepDefinition>(_stepsByKey.Count, StringComparer.Ordinal);
        foreach (var pair in _stepsByKey)
        {
            var transitions = pair.Value.Transitions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Key);
            steps[pair.Key] = new FlowStepDefinition(pair.Value.StepType, transitions);
        }

        ValidateTransitions(steps);
        ValidateAcyclic(steps);

        return new FlowDefinition<TContext>(_flowName, _startStepKey, steps);
    }

    private static void ValidateTransitions(Dictionary<string, FlowStepDefinition> steps)
    {
        foreach (var definition in steps.Values)
        {
            foreach (var targetKey in definition.Transitions.Values)
            {
                if (!steps.ContainsKey(targetKey))
                {
                    throw new ApplicationLifecycleException($"Transition references undefined step '{targetKey}'.");
                }
            }
        }
    }

    private void ValidateAcyclic(IReadOnlyDictionary<string, FlowStepDefinition> steps)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in steps.Keys)
        {
            if (!visited.Contains(key))
            {
                DepthFirstSearch(key, steps, visiting, visited);
            }
        }
    }

    private void DepthFirstSearch(
        string key,
        IReadOnlyDictionary<string, FlowStepDefinition> steps,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        visiting.Add(key);

        foreach (var next in steps[key].Transitions.Values)
        {
            if (visiting.Contains(next))
            {
                throw new ApplicationLifecycleException($"Cycle detected in flow '{_flowName}' involving step '{steps[key].StepType.FullName}'.");
            }

            if (!visited.Contains(next))
            {
                DepthFirstSearch(next, steps, visiting, visited);
            }
        }

        visiting.Remove(key);
        visited.Add(key);
    }
}
