using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Flows;

/// <summary>
/// Represents a unit of work within a flow.
/// </summary>
/// <typeparam name="TContext">Flow context type.</typeparam>
public interface IFlowStep<in TContext> where TContext : IFlowContext
{
    /// <summary>
    /// Executes the step asynchronously.
    /// </summary>
    Task<FlowOutcome> ExecuteAsync(TContext context, CancellationToken cancellationToken);
}
