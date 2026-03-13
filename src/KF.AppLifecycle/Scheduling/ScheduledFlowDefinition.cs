using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;

namespace KoreForge.AppLifecycle.Scheduling;

/// <summary>
/// Internal representation of a scheduled flow binding.
/// </summary>
internal sealed class ScheduledFlowDefinition
{
    public ScheduledFlowDefinition(
        string flowName,
        Type triggerType,
        FlowDefinition<ScheduledContext> flow,
        bool noOverlap)
    {
        FlowName = flowName;
        TriggerType = triggerType;
        Flow = flow;
        NoOverlap = noOverlap;
    }

    public string FlowName { get; }

    public Type TriggerType { get; }

    public FlowDefinition<ScheduledContext> Flow { get; }

    public bool NoOverlap { get; }
}
