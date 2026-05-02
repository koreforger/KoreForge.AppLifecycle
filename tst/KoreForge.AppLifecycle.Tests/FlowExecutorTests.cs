using System;
using System.Collections.Generic;
using KoreForge.AppLifecycle.Events;
using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Options;
using KoreForge.AppLifecycle.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KoreForge.AppLifecycle.Tests;

public sealed class FlowExecutorTests
{
    private readonly ILogger _logger = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug)).CreateLogger("FlowExecutorTests");
    private readonly TestHostEnvironment _environment = new();

    [Fact]
    public async Task ExecuteAsync_CompletesSuccessFlow()
    {
        using var provider = BuildServices(services => services.AddTransient<SuccessStep>());
        var options = new ApplicationLifecycleOptions();
        options.Startup.Flow("Success")
            .BeginWith<SuccessStep>()
            .EndFlow();

        var flow = options.BuildStartupFlows().Single();
        var executor = new FlowExecutor<StartupContext>();
        var context = new StartupContext(provider, _environment);
        var eventsHub = new ApplicationLifecycleEvents(options);

        var outcome = await executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None);

        Assert.Equal(FlowOutcome.Success, outcome);
    }

    [Fact]
    public async Task ExecuteAsync_ConvertsExceptionsToFailure()
    {
        using var provider = BuildServices(services =>
        {
            services.AddTransient<ThrowingStep>();
        });

        var options = new ApplicationLifecycleOptions();
        options.Startup.Flow("Throws")
            .BeginWith<ThrowingStep>()
            .EndFlow();

        var flow = options.BuildStartupFlows().Single();
        var executor = new FlowExecutor<StartupContext>();
        var context = new StartupContext(provider, _environment);
        var eventsHub = new ApplicationLifecycleEvents(options);

        var outcome = await executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None);

        Assert.Equal(FlowOutcome.Failure, outcome);
    }

    [Fact]
    public async Task ExecuteAsync_TreatsUnmappedOutcomeAsFailureWhenConfigured()
    {
        using var provider = BuildServices(services =>
        {
            services.AddTransient<CustomOutcomeStep>();
            services.AddTransient<SuccessStep>();
        });

        var options = new ApplicationLifecycleOptions
        {
            UnmappedOutcomePolicy = UnmappedOutcomePolicy.TreatAsFailure
        };

        options.Startup.Flow("CustomOutcome")
            .BeginWith<CustomOutcomeStep>()
                .IfFailure().Then<SuccessStep>()
            .EndFlow();

        var flow = options.BuildStartupFlows().Single();
        var executor = new FlowExecutor<StartupContext>();
        var context = new StartupContext(provider, _environment);
        var eventsHub = new ApplicationLifecycleEvents(options);

        var outcome = await executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None);

        Assert.Equal(FlowOutcome.Success, outcome);
    }

    [Fact]
    public async Task ExecuteAsync_UnmappedOutcomeThrowsWhenPolicySet()
    {
        using var provider = BuildServices(services => services.AddTransient<CustomOutcomeStep>());

        var options = new ApplicationLifecycleOptions
        {
            UnmappedOutcomePolicy = UnmappedOutcomePolicy.Throw
        };

        options.Startup.Flow("CustomOutcome")
            .BeginWith<CustomOutcomeStep>()
            .EndFlow();

        var flow = options.BuildStartupFlows().Single();
        var executor = new FlowExecutor<StartupContext>();
        var context = new StartupContext(provider, _environment);
        var eventsHub = new ApplicationLifecycleEvents(options);

        await Assert.ThrowsAsync<ApplicationLifecycleException>(() => executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_FollowsIfThenElseBranches()
    {
        var tracker = new BranchTracker();

        using var provider = BuildServices(services =>
        {
            services.AddSingleton(tracker);
            services.AddTransient<BranchingStep>();
            services.AddTransient<SuccessBranchStep>();
            services.AddTransient<FailureBranchStep>();
        });

        var options = new ApplicationLifecycleOptions();
        options.Startup.Flow("Branches")
            .BeginWith<BranchingStep>()
                .IfSuccess().Then<SuccessBranchStep>()
                .IfFailure().Then<FailureBranchStep>()
            .EndFlow();

        var flow = options.BuildStartupFlows().Single();
        var executor = new FlowExecutor<StartupContext>();
        var eventsHub = new ApplicationLifecycleEvents(options);

        // Success path
        tracker.ForceFailure = false;
        var context = new StartupContext(provider, _environment);
        var successOutcome = await executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None);

        Assert.Equal(FlowOutcome.Success, successOutcome);
        Assert.True(tracker.SuccessBranchVisited);
        Assert.False(tracker.FailureBranchVisited);

        // Failure path
        tracker.Reset();
        tracker.ForceFailure = true;
        context = new StartupContext(provider, _environment);
        var failureOutcome = await executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None);

        Assert.Equal(FlowOutcome.Failure, failureOutcome);
        Assert.False(tracker.SuccessBranchVisited);
        Assert.True(tracker.FailureBranchVisited);
    }

    [Fact]
    public async Task ExecuteAsync_ChainsMultipleBranchOutcomes()
    {
        var tracker = new ChainTracker();

        using var provider = BuildServices(services =>
        {
            services.AddSingleton(tracker);
            services.AddTransient<PrimaryDecisionStep>();
            services.AddTransient<AlternateBranchStep>();
            services.AddTransient<FinalSuccessStep>();
            services.AddTransient<FailureTerminalStep>();
        });

        var options = new ApplicationLifecycleOptions();
        options.Startup.Flow("Nested")
            .BeginWith<PrimaryDecisionStep>()
                .IfFailure().Then<FailureTerminalStep>()
                .Then<AlternateBranchStep>()
                    .IfSuccess().Then<FinalSuccessStep>()
                    .IfFailure().Then<FailureTerminalStep>()
            .EndFlow();

        var flow = options.BuildStartupFlows().Single();
        var executor = new FlowExecutor<StartupContext>();
        var eventsHub = new ApplicationLifecycleEvents(options);

        tracker.PrimaryShouldFail = false;
        tracker.AlternateShouldFail = false;

        var context = new StartupContext(provider, _environment);
        var outcome = await executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None);

        Assert.Equal(FlowOutcome.Success, outcome);
        Assert.Equal(new[] { "Primary", "Alternate", "Final" }, tracker.VisitedSteps);

        tracker.Reset();
        tracker.PrimaryShouldFail = true;

        context = new StartupContext(provider, _environment);
        outcome = await executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None);

    Assert.Equal(FlowOutcome.Failure, outcome);
    Assert.Equal(new[] { "Primary", "Failure" }, tracker.VisitedSteps);

        tracker.Reset();
        tracker.PrimaryShouldFail = false;
        tracker.AlternateShouldFail = true;

        context = new StartupContext(provider, _environment);
        outcome = await executor.ExecuteAsync(
            flow,
            context,
            options.UnmappedOutcomePolicy,
            eventsHub,
            LifecycleSection.Startup,
            _logger,
            logStepExceptions: true,
            logUnmappedOutcomes: true,
            CancellationToken.None);

        Assert.Equal(FlowOutcome.Failure, outcome);
        Assert.Equal(new[] { "Primary", "Alternate", "Failure" }, tracker.VisitedSteps);
    }

    private static ServiceProvider BuildServices(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private sealed class SuccessStep : IFlowStep<StartupContext>
    {
        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
            => Task.FromResult(FlowOutcome.Success);
    }

    private sealed class ThrowingStep : IFlowStep<StartupContext>
    {
        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }

    private sealed class CustomOutcomeStep : IFlowStep<StartupContext>
    {
        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
            => Task.FromResult(FlowOutcome.Custom("Custom"));
    }

    private sealed class BranchTracker
    {
        public bool ForceFailure { get; set; }
        public bool SuccessBranchVisited { get; private set; }
        public bool FailureBranchVisited { get; private set; }

        public void VisitSuccess() => SuccessBranchVisited = true;
        public void VisitFailure() => FailureBranchVisited = true;
        public void Reset()
        {
            SuccessBranchVisited = false;
            FailureBranchVisited = false;
        }
    }

    private sealed class BranchingStep : IFlowStep<StartupContext>
    {
        private readonly BranchTracker _tracker;

        public BranchingStep(BranchTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
            => Task.FromResult(_tracker.ForceFailure ? FlowOutcome.Failure : FlowOutcome.Success);
    }

    private sealed class SuccessBranchStep : IFlowStep<StartupContext>
    {
        private readonly BranchTracker _tracker;

        public SuccessBranchStep(BranchTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
        {
            _tracker.VisitSuccess();
            return Task.FromResult(FlowOutcome.Success);
        }
    }

    private sealed class FailureBranchStep : IFlowStep<StartupContext>
    {
        private readonly BranchTracker _tracker;

        public FailureBranchStep(BranchTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
        {
            _tracker.VisitFailure();
            return Task.FromResult(FlowOutcome.Failure);
        }
    }

    private sealed class ChainTracker
    {
        private readonly List<string> _visited = new();

        public bool PrimaryShouldFail { get; set; }
        public bool AlternateShouldFail { get; set; }

        public IReadOnlyList<string> VisitedSteps => _visited.ToArray();

        public void Add(string step) => _visited.Add(step);

        public void Reset()
        {
            _visited.Clear();
            PrimaryShouldFail = false;
            AlternateShouldFail = false;
        }
    }

    private sealed class PrimaryDecisionStep : IFlowStep<StartupContext>
    {
        private readonly ChainTracker _tracker;

        public PrimaryDecisionStep(ChainTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
        {
            _tracker.Add("Primary");
            return Task.FromResult(_tracker.PrimaryShouldFail ? FlowOutcome.Failure : FlowOutcome.Success);
        }
    }

    private sealed class AlternateBranchStep : IFlowStep<StartupContext>
    {
        private readonly ChainTracker _tracker;

        public AlternateBranchStep(ChainTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
        {
            _tracker.Add("Alternate");
            return Task.FromResult(_tracker.AlternateShouldFail ? FlowOutcome.Failure : FlowOutcome.Success);
        }
    }

    private sealed class FinalSuccessStep : IFlowStep<StartupContext>
    {
        private readonly ChainTracker _tracker;

        public FinalSuccessStep(ChainTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
        {
            _tracker.Add("Final");
            return Task.FromResult(FlowOutcome.Success);
        }
    }

    private sealed class FailureTerminalStep : IFlowStep<StartupContext>
    {
        private readonly ChainTracker _tracker;

        public FailureTerminalStep(ChainTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
        {
            _tracker.Add("Failure");
            return Task.FromResult(FlowOutcome.Failure);
        }
    }
}
