using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Internal;
using KoreForge.AppLifecycle.Scheduling;

namespace KoreForge.AppLifecycle.Options;

/// <summary>
/// Fluent surface for configuring scheduled flows.
/// </summary>
public sealed class ScheduledFlowConfigurator
{
    private readonly FlowConfiguratorCore<ScheduledContext> _core = new();
    private readonly List<ScheduledFlowDefinition> _definitions = new();

    /// <summary>
    /// Begins configuring a scheduled flow.
    /// </summary>
    public ScheduledFlowBuilder Flow(string name)
    {
        var builderCore = _core.CreateFlow(name);
        return new ScheduledFlowBuilder(this, name, builderCore);
    }

    internal void Register(ScheduledFlowDefinition definition)
    {
        _definitions.Add(definition);
    }

    internal IReadOnlyList<ScheduledFlowDefinition> BuildDefinitions() => _definitions.AsReadOnly();
}

/// <summary>
/// Builds the steps and metadata for a scheduled flow.
/// </summary>
public sealed class ScheduledFlowBuilder
{
    private readonly ScheduledFlowConfigurator _owner;
    private readonly string _flowName;
    private readonly FlowBuilderCore<ScheduledContext> _core;
    private Type? _triggerType;
    private bool _noOverlap;

    internal ScheduledFlowBuilder(ScheduledFlowConfigurator owner, string flowName, FlowBuilderCore<ScheduledContext> core)
    {
        _owner = owner;
        _flowName = flowName;
        _core = core;
    }

    /// <summary>
    /// Assigns the trigger type that provides delays between runs.
    /// </summary>
    public ScheduledFlowBuilder OnSchedule<TTrigger>() where TTrigger : class, IScheduleTrigger
    {
        var triggerType = typeof(TTrigger);
        if (_triggerType is not null && _triggerType != triggerType)
        {
            throw new ApplicationLifecycleException($"Scheduled flow '{_flowName}' already has trigger '{_triggerType.Name}'.");
        }

        _triggerType = triggerType;
        return this;
    }

    /// <summary>
    /// Prevents overlapping executions of this flow.
    /// </summary>
    public ScheduledFlowBuilder NoOverlap()
    {
        _noOverlap = true;
        return this;
    }

    /// <summary>
    /// Declares the first step for the scheduled flow.
    /// </summary>
    public ScheduledStepBuilder BeginWith<TStep>() where TStep : class, IFlowStep<ScheduledContext>
    {
        var stepCore = _core.BeginWith(typeof(TStep));
        return new ScheduledStepBuilder(this, stepCore);
    }

    /// <summary>
    /// Completes the scheduled flow definition.
    /// </summary>
    public ScheduledFlowBuilder EndFlow()
    {
        var definition = _core.Complete();
        FinalizeFlow(definition);
        return this;
    }

    internal ScheduledFlowBuilder CompleteFromStep(FlowStepBuilderCore<ScheduledContext> stepCore)
    {
        var definition = stepCore.CompleteFlow();
        FinalizeFlow(definition);
        return this;
    }

    private void FinalizeFlow(FlowDefinition<ScheduledContext> definition)
    {
        if (_triggerType is null)
        {
            throw new ApplicationLifecycleException($"Scheduled flow '{_flowName}' must define a trigger via OnSchedule().");
        }

        var scheduled = new ScheduledFlowDefinition(_flowName, _triggerType, definition, _noOverlap);
        _owner.Register(scheduled);
    }
}

/// <summary>
/// Configures transitions for a scheduled flow step.
/// </summary>
public sealed class ScheduledStepBuilder
{
    private readonly ScheduledFlowBuilder _builder;
    private FlowStepBuilderCore<ScheduledContext> _core;

    internal ScheduledStepBuilder(ScheduledFlowBuilder builder, FlowStepBuilderCore<ScheduledContext> core)
    {
        _builder = builder;
        _core = core;
    }

    /// <summary>
    /// Selects the outcome to attach a transition to.
    /// </summary>
    public ScheduledStepBuilder If(FlowOutcome outcome)
    {
        _core = _core.If(outcome);
        return this;
    }

    /// <summary>
    /// Shortcut for mapping <see cref="FlowOutcome.Success"/>.
    /// </summary>
    public ScheduledStepBuilder IfSuccess() => If(FlowOutcome.Success);

    /// <summary>
    /// Shortcut for mapping <see cref="FlowOutcome.Failure"/>.
    /// </summary>
    public ScheduledStepBuilder IfFailure() => If(FlowOutcome.Failure);

    /// <summary>
    /// Adds a transition to the specified step.
    /// </summary>
    public ScheduledStepBuilder Then<TNextStep>() where TNextStep : class, IFlowStep<ScheduledContext>
    {
        _core = _core.Then(typeof(TNextStep));
        return this;
    }

    /// <summary>
    /// Finishes the scheduled flow configuration.
    /// </summary>
    public ScheduledFlowBuilder EndFlow()
    {
        return _builder.CompleteFromStep(_core);
    }
}
