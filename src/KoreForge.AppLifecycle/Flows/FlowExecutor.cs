using System.Diagnostics;
using KoreForge.AppLifecycle.Events;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Internal;
using KoreForge.AppLifecycle.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KoreForge.AppLifecycle.Flows;

internal sealed class FlowExecutor<TContext> where TContext : IFlowContext
{
    public async Task<FlowOutcome> ExecuteAsync(
        FlowDefinition<TContext> flow,
        TContext context,
        UnmappedOutcomePolicy unmappedOutcomePolicy,
        ApplicationLifecycleEvents eventHub,
        LifecycleSection? section,
        ILogger logger,
        bool logStepExceptions,
        bool logUnmappedOutcomes,
        CancellationToken cancellationToken)
    {
        var currentKey = flow.StartStepKey;
        var lastOutcome = FlowOutcome.Success;

        while (currentKey != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!flow.Steps.TryGetValue(currentKey, out var stepDefinition))
            {
                throw new ApplicationLifecycleException($"Step '{currentKey}' not found within flow '{flow.Name}'.");
            }

            if (section is LifecycleSection executingSection)
            {
                var (executingHandler, _) = eventHub.GetStepHandlers(executingSection);
                var executingArgs = new LifecycleStepExecutingEventArgs(context.Services, executingSection, flow.Name, stepDefinition.StepType);
                await EventDispatcher.InvokeAsync(executingHandler, executingArgs, logger, $"{executingSection}StepExecuting").ConfigureAwait(false);
            }

            FlowOutcome outcome;
            Exception? exception = null;
            var stopwatch = Stopwatch.StartNew();

            using var scope = context.Services.CreateScope();
            var step = (IFlowStep<TContext>)scope.ServiceProvider.GetRequiredService(stepDefinition.StepType);

            try
            {
                outcome = await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
                outcome = FlowOutcome.Failure;
                if (logStepExceptions)
                {
                    logger.LogError(ex, "Flow '{Flow}' step '{Step}' threw an exception.", flow.Name, stepDefinition.StepType.FullName);
                }
            }

            stopwatch.Stop();

            if (exception is null && outcome == FlowOutcome.Failure)
            {
                logger.LogWarning("Flow '{Flow}' step '{Step}' returned Failure.", flow.Name, stepDefinition.StepType.FullName);
            }

            if (section is LifecycleSection executedSection)
            {
                var (_, executedHandler) = eventHub.GetStepHandlers(executedSection);
                var executedArgs = new LifecycleStepExecutedEventArgs(
                    context.Services,
                    executedSection,
                    flow.Name,
                    stepDefinition.StepType,
                    outcome,
                    stopwatch.Elapsed,
                    exception);

                await EventDispatcher.InvokeAsync(executedHandler, executedArgs, logger, $"{executedSection}StepExecuted").ConfigureAwait(false);
            }

            lastOutcome = outcome;

            if (stepDefinition.Transitions.TryGetValue(outcome, out var nextStepKey))
            {
                currentKey = nextStepKey;
                continue;
            }

            switch (unmappedOutcomePolicy)
            {
                case UnmappedOutcomePolicy.StopFlow:
                    if (logUnmappedOutcomes)
                    {
                        logger.LogWarning(
                            "Flow '{Flow}' step '{Step}' returned unmapped outcome '{Outcome}'. Flow will stop.",
                            flow.Name,
                            stepDefinition.StepType.FullName,
                            outcome);
                    }

                    currentKey = null;
                    break;

                case UnmappedOutcomePolicy.TreatAsFailure:
                    if (!stepDefinition.Transitions.TryGetValue(FlowOutcome.Failure, out var failureStep))
                    {
                        if (logUnmappedOutcomes)
                        {
                            logger.LogWarning(
                                "Flow '{Flow}' step '{Step}' cannot treat unmapped outcome '{Outcome}' as Failure because no Failure transition exists.",
                                flow.Name,
                                stepDefinition.StepType.FullName,
                                outcome);
                        }

                        currentKey = null;
                    }
                    else
                    {
                        currentKey = failureStep;
                    }

                    break;

                case UnmappedOutcomePolicy.Throw:
                    throw new ApplicationLifecycleException($"Flow '{flow.Name}' outcome '{outcome}' from step '{stepDefinition.StepType.FullName}' is not mapped.");

                default:
                    currentKey = null;
                    break;
            }
        }

        return lastOutcome;
    }
}
