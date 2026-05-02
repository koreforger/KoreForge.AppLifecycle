using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Internal;

namespace KoreForge.AppLifecycle.Options;

/// <summary>
/// Fluent surface for configuring shutdown flows.
/// </summary>
public sealed class ShutdownFlowConfigurator
{
    private readonly FlowConfiguratorCore<ShutdownContext> _core = new();

    /// <summary>
    /// Begins configuring a shutdown flow with the specified name.
    /// </summary>
    public ShutdownFlowBuilder Flow(string name)
    {
        var builderCore = _core.CreateFlow(name);
        return new ShutdownFlowBuilder(builderCore);
    }

    internal IReadOnlyList<FlowDefinition<ShutdownContext>> BuildDefinitions() => _core.Build();
}

/// <summary>
/// Builds the set of steps for a shutdown flow.
/// </summary>
public sealed class ShutdownFlowBuilder
{
    private readonly FlowBuilderCore<ShutdownContext> _core;

    internal ShutdownFlowBuilder(FlowBuilderCore<ShutdownContext> core)
    {
        _core = core;
    }

    /// <summary>
    /// Declares the first step for the shutdown flow.
    /// </summary>
    public ShutdownStepBuilder BeginWith<TStep>() where TStep : class, IFlowStep<ShutdownContext>
    {
        var stepCore = _core.BeginWith(typeof(TStep));
        return new ShutdownStepBuilder(this, stepCore);
    }

    /// <summary>
    /// Completes the shutdown flow definition.
    /// </summary>
    public ShutdownFlowBuilder EndFlow()
    {
        _core.Complete();
        return this;
    }
}

/// <summary>
/// Configures transitions for a shutdown step.
/// </summary>
public sealed class ShutdownStepBuilder
{
    private readonly ShutdownFlowBuilder _builder;
    private FlowStepBuilderCore<ShutdownContext> _core;

    internal ShutdownStepBuilder(ShutdownFlowBuilder builder, FlowStepBuilderCore<ShutdownContext> core)
    {
        _builder = builder;
        _core = core;
    }

    /// <summary>
    /// Selects the outcome to attach a transition to.
    /// </summary>
    public ShutdownStepBuilder If(FlowOutcome outcome)
    {
        _core = _core.If(outcome);
        return this;
    }

    /// <summary>
    /// Shortcut for selecting the success outcome.
    /// </summary>
    public ShutdownStepBuilder IfSuccess() => If(FlowOutcome.Success);

    /// <summary>
    /// Shortcut for selecting the failure outcome.
    /// </summary>
    public ShutdownStepBuilder IfFailure() => If(FlowOutcome.Failure);

    /// <summary>
    /// Adds a transition to the specified next step.
    /// </summary>
    public ShutdownStepBuilder Then<TNextStep>() where TNextStep : class, IFlowStep<ShutdownContext>
    {
        _core = _core.Then(typeof(TNextStep));
        return this;
    }

    /// <summary>
    /// Finishes the flow configuration.
    /// </summary>
    public ShutdownFlowBuilder EndFlow()
    {
        _core.CompleteFlow();
        return _builder;
    }
}
