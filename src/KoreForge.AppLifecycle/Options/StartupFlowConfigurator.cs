using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Internal;

namespace KoreForge.AppLifecycle.Options;

/// <summary>
/// Fluent surface for configuring startup flows.
/// </summary>
public sealed class StartupFlowConfigurator
{
    private readonly FlowConfiguratorCore<StartupContext> _core = new();

    /// <summary>
    /// Begins configuring a startup flow with the specified name.
    /// </summary>
    public StartupFlowBuilder Flow(string name)
    {
        var builderCore = _core.CreateFlow(name);
        return new StartupFlowBuilder(builderCore);
    }

    internal IReadOnlyList<FlowDefinition<StartupContext>> BuildDefinitions() => _core.Build();
}

/// <summary>
/// Builds the set of steps for a startup flow.
/// </summary>
public sealed class StartupFlowBuilder
{
    private readonly FlowBuilderCore<StartupContext> _core;

    internal StartupFlowBuilder(FlowBuilderCore<StartupContext> core)
    {
        _core = core;
    }

    /// <summary>
    /// Declares the first step of the flow.
    /// </summary>
    public StartupStepBuilder BeginWith<TStep>() where TStep : class, IFlowStep<StartupContext>
    {
        var stepCore = _core.BeginWith(typeof(TStep));
        return new StartupStepBuilder(this, stepCore);
    }

    /// <summary>
    /// Completes the flow definition.
    /// </summary>
    public StartupFlowBuilder EndFlow()
    {
        _core.Complete();
        return this;
    }
}

/// <summary>
/// Configures transitions for a specific startup step.
/// </summary>
public sealed class StartupStepBuilder
{
    private readonly StartupFlowBuilder _builder;
    private FlowStepBuilderCore<StartupContext> _core;

    internal StartupStepBuilder(StartupFlowBuilder builder, FlowStepBuilderCore<StartupContext> core)
    {
        _builder = builder;
        _core = core;
    }

    /// <summary>
    /// Selects the outcome to attach the next transition to.
    /// </summary>
    public StartupStepBuilder If(FlowOutcome outcome)
    {
        _core = _core.If(outcome);
        return this;
    }

    /// <summary>
    /// Shortcut for mapping the success outcome.
    /// </summary>
    public StartupStepBuilder IfSuccess() => If(FlowOutcome.Success);

    /// <summary>
    /// Shortcut for mapping the failure outcome.
    /// </summary>
    public StartupStepBuilder IfFailure() => If(FlowOutcome.Failure);

    /// <summary>
    /// Adds a transition to the specified next step.
    /// </summary>
    public StartupStepBuilder Then<TNextStep>() where TNextStep : class, IFlowStep<StartupContext>
    {
        _core = _core.Then(typeof(TNextStep));
        return this;
    }

    /// <summary>
    /// Finishes the flow configuration.
    /// </summary>
    public StartupFlowBuilder EndFlow()
    {
        _core.CompleteFlow();
        return _builder;
    }
}
