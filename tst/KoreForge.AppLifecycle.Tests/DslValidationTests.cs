using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Options;

namespace KoreForge.AppLifecycle.Tests;

public sealed class DslValidationTests
{
    [Fact]
    public void StartupFlow_WithDuplicateNames_Throws()
    {
        var options = new ApplicationLifecycleOptions();
        options.Startup.Flow("Duplicate").BeginWith<StartupStubStep>().EndFlow();

        Assert.Throws<ApplicationLifecycleException>(() => options.Startup.Flow("Duplicate"));
    }

    [Fact]
    public void StartupFlow_WithoutBeginWith_Throws()
    {
        var options = new ApplicationLifecycleOptions();
        var builder = options.Startup.Flow("NoBegin");
        Assert.Throws<ApplicationLifecycleException>(() => builder.EndFlow());
    }

    [Fact]
    public void StartupFlow_WithCycles_Throws()
    {
        var options = new ApplicationLifecycleOptions();

        Assert.Throws<ApplicationLifecycleException>(() =>
        {
            options.Startup.Flow("Cycle")
                .BeginWith<StartupStubStep>()
                    .Then<StartupOtherStep>()
                    .Then<StartupStubStep>()
                .EndFlow();
        });
    }

    [Fact]
    public void ScheduledFlow_MustDeclareTrigger()
    {
        var options = new ApplicationLifecycleOptions();
        var builder = options.Scheduled.Flow("MissingTrigger").BeginWith<ScheduledStubStep>();
        Assert.Throws<ApplicationLifecycleException>(() => builder.EndFlow());
    }

    private sealed class StartupStubStep : IFlowStep<StartupContext>
    {
        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
            => Task.FromResult(FlowOutcome.Success);
    }

    private sealed class StartupOtherStep : IFlowStep<StartupContext>
    {
        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
            => Task.FromResult(FlowOutcome.Success);
    }

    private sealed class ScheduledStubStep : IFlowStep<ScheduledContext>
    {
        public Task<FlowOutcome> ExecuteAsync(ScheduledContext context, CancellationToken cancellationToken)
            => Task.FromResult(FlowOutcome.Success);
    }
}
