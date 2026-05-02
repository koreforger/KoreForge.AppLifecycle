using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Internal;

internal sealed class FlowConfiguratorCore<TContext> where TContext : IFlowContext
{
    private readonly List<FlowDefinition<TContext>> _definitions = new();
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);

    public FlowBuilderCore<TContext> CreateFlow(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApplicationLifecycleException("Flow name cannot be null or whitespace.");
        }

        if (!_names.Add(name))
        {
            throw new ApplicationLifecycleException($"Flow '{name}' is already defined.");
        }

        return new FlowBuilderCore<TContext>(this, name);
    }

    internal void AddDefinition(FlowDefinition<TContext> definition)
    {
        _definitions.Add(definition);
    }

    public IReadOnlyList<FlowDefinition<TContext>> Build() => _definitions.AsReadOnly();
}
