using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Flows;

/// <summary>
/// Immutable description of a flow.
/// </summary>
/// <typeparam name="TContext">Context type.</typeparam>
public sealed class FlowDefinition<TContext> where TContext : IFlowContext
{
    internal FlowDefinition(
        string name,
        string startStepKey,
        IReadOnlyDictionary<string, FlowStepDefinition> steps)
    {
        Name = name;
        StartStepKey = startStepKey;
        Steps = steps;
    }

    public string Name { get; }

    internal string StartStepKey { get; }

    internal IReadOnlyDictionary<string, FlowStepDefinition> Steps { get; }
}
