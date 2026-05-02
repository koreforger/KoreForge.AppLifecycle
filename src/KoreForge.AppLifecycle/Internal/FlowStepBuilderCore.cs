using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Internal;

internal sealed class FlowStepBuilderCore<TContext> where TContext : IFlowContext
{
    private readonly FlowBuilderCore<TContext> _flowBuilder;
    private readonly FlowBuilderState<TContext> _state;
    private readonly FlowStepBuilderState _currentStep;
    private readonly FlowOutcome? _pendingOutcome;

    public FlowStepBuilderCore(
        FlowBuilderCore<TContext> flowBuilder,
        FlowBuilderState<TContext> state,
        FlowStepBuilderState currentStep,
        FlowOutcome? pendingOutcome)
    {
        _flowBuilder = flowBuilder;
        _state = state;
        _currentStep = currentStep;
        _pendingOutcome = pendingOutcome;
    }

    public FlowStepBuilderCore<TContext> If(FlowOutcome outcome)
    {
        return new FlowStepBuilderCore<TContext>(_flowBuilder, _state, _currentStep, outcome);
    }

    public FlowStepBuilderCore<TContext> Then(Type stepType)
    {
        _flowBuilder.EnsureAssignable(stepType);
        var nextStep = _state.GetOrCreateStep(stepType);
        var outcome = _pendingOutcome ?? FlowOutcome.Success;
        var shouldMove = _pendingOutcome is null;
        _state.AddTransition(_currentStep, outcome, nextStep);

        return shouldMove
            ? new FlowStepBuilderCore<TContext>(_flowBuilder, _state, nextStep, null)
            : new FlowStepBuilderCore<TContext>(_flowBuilder, _state, _currentStep, null);
    }

    public FlowDefinition<TContext> CompleteFlow()
    {
        return _flowBuilder.Complete();
    }
}
