using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Internal;

internal sealed class FlowBuilderCore<TContext> where TContext : IFlowContext
{
    private readonly FlowConfiguratorCore<TContext> _owner;
    private readonly FlowBuilderState<TContext> _state;
    private bool _completed;

    public FlowBuilderCore(FlowConfiguratorCore<TContext> owner, string name)
    {
        _owner = owner;
        _state = new FlowBuilderState<TContext>(name);
    }

    public FlowStepBuilderCore<TContext> BeginWith(Type stepType)
    {
        EnsureAssignable(stepType);
        var stepState = _state.BeginWith(stepType);
        return new FlowStepBuilderCore<TContext>(this, _state, stepState, null);
    }

    public void EnsureAssignable(Type stepType)
    {
        if (!typeof(IFlowStep<TContext>).IsAssignableFrom(stepType))
        {
            throw new ApplicationLifecycleException($"Type '{stepType.FullName}' does not implement IFlowStep<{typeof(TContext).Name}>.");
        }
    }

    public FlowDefinition<TContext> Complete()
    {
        if (_completed)
        {
            throw new ApplicationLifecycleException("Flow already completed.");
        }

        var definition = _state.Build();
        _owner.AddDefinition(definition);
        _completed = true;
        return definition;
    }

    internal FlowStepBuilderCore<TContext> CreateStepBuilder(FlowStepBuilderState stepState)
        => new(this, _state, stepState, null);
}
