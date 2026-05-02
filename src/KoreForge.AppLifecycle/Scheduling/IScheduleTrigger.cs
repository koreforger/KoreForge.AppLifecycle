using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Scheduling;

/// <summary>
/// Provides the delay between scheduled flow executions.
/// </summary>
public interface IScheduleTrigger
{
    /// <summary>
    /// Calculates the delay until the next execution.
    /// </summary>
    Task<TimeSpan> GetNextDelayAsync(ScheduledContext context, CancellationToken cancellationToken);
}
