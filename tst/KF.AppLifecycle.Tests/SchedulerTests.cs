using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KoreForge.AppLifecycle.Events;
using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Options;
using KoreForge.AppLifecycle.Scheduling;
using KoreForge.AppLifecycle.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KoreForge.AppLifecycle.Tests;

public sealed class SchedulerTests
{
    [Fact]
    public async Task RunScheduledLoopAsync_ExecutesSteps()
    {
        var services = new ServiceCollection();
        var state = new SchedulerState { MaxExecutions = 1 };
        services.AddSingleton(state);
        services.AddTransient<CountingScheduledStep>();
        services.AddTransient<ZeroDelayTrigger>();
        using var provider = services.BuildServiceProvider();

        var options = new ApplicationLifecycleOptions();
        options.Scheduled.Flow("Counting")
            .OnSchedule<ZeroDelayTrigger>()
            .BeginWith<CountingScheduledStep>()
            .EndFlow();

        var definition = options.BuildScheduledFlows().Single();
        var logger = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug)).CreateLogger<ScheduledTasksHostedService>();
        var service = new ScheduledTasksHostedService(provider, new TestHostEnvironment(), options, new ApplicationLifecycleEvents(options), logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        state.Cancellation = cts;

        await service.RunScheduledLoopAsync(definition, cts.Token);

        Assert.True(state.ExecutionCount >= 1);
    }

    [Fact]
    public async Task RunScheduledLoopAsync_SkipsWhenOverlapGuardHeld()
    {
        var services = new ServiceCollection();
        var state = new SchedulerState { MaxExecutions = 1 };
        services.AddSingleton(state);
        services.AddTransient<CountingScheduledStep>();
        services.AddTransient<ZeroDelayTrigger>();
        using var provider = services.BuildServiceProvider();

        var options = new ApplicationLifecycleOptions();
        options.Scheduled.Flow("Guarded")
            .OnSchedule<ZeroDelayTrigger>()
            .NoOverlap()
            .BeginWith<CountingScheduledStep>()
            .EndFlow();

        var definition = options.BuildScheduledFlows().Single();
        var logger = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug)).CreateLogger<ScheduledTasksHostedService>();
        var service = new ScheduledTasksHostedService(provider, new TestHostEnvironment(), options, new ApplicationLifecycleEvents(options), logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var gate = service.GetOrCreateGate(definition.FlowName);
        await gate.WaitAsync(cts.Token);

        var runTask = service.RunScheduledLoopAsync(definition, cts.Token);
        await Task.Delay(30, CancellationToken.None);
        cts.Cancel();
        await runTask;

        gate.Release();
        Assert.Equal(0, state.ExecutionCount);
    }

    [Fact]
    public async Task RunScheduledLoopAsync_UsesFallbackDelayWhenTriggerFails()
    {
        var services = new ServiceCollection();
        var state = new SchedulerState { MaxExecutions = 1 };
        services.AddSingleton(state);
        services.AddTransient<CountingScheduledStep>();
        services.AddTransient<FailingTrigger>();
        using var provider = services.BuildServiceProvider();

        var options = new ApplicationLifecycleOptions();
        options.Scheduled.Flow("Fallback")
            .OnSchedule<FailingTrigger>()
            .BeginWith<CountingScheduledStep>()
            .EndFlow();

        var definition = options.BuildScheduledFlows().Single();
        var logger = new TestLogger<ScheduledTasksHostedService>();
        var service = new ScheduledTasksHostedService(
            provider,
            new TestHostEnvironment(),
            options,
            new ApplicationLifecycleEvents(options),
            logger,
            TimeSpan.FromMilliseconds(10));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        state.Cancellation = cts;

        await service.RunScheduledLoopAsync(definition, cts.Token);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("trigger"));
        Assert.Equal(1, state.ExecutionCount);
    }

    private sealed class SchedulerState
    {
        private int _executionCount;
        private int _concurrent;

        public int ExecutionCount => _executionCount;
        public int MaxExecutions { get; set; } = int.MaxValue;
        public CancellationTokenSource? Cancellation { get; set; }
        public TimeSpan StepDelay { get; set; } = TimeSpan.FromMilliseconds(20);

        public int IncrementExecutions()
        {
            return Interlocked.Increment(ref _executionCount);
        }

        public void IncrementConcurrency()
        {
            Interlocked.Increment(ref _concurrent);
        }

        public void DecrementConcurrency()
        {
            Interlocked.Decrement(ref _concurrent);
        }
    }

    private sealed class CountingScheduledStep : IFlowStep<ScheduledContext>
    {
        private readonly SchedulerState _state;

        public CountingScheduledStep(SchedulerState state)
        {
            _state = state;
        }

        public async Task<FlowOutcome> ExecuteAsync(ScheduledContext context, CancellationToken cancellationToken)
        {
            _state.IncrementConcurrency();
            try
            {
                var executions = _state.IncrementExecutions();
                if (executions >= _state.MaxExecutions)
                {
                    _state.Cancellation?.Cancel();
                }

                await Task.Delay(_state.StepDelay, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _state.DecrementConcurrency();
            }

            return FlowOutcome.Success;
        }
    }

    private sealed class ZeroDelayTrigger : IScheduleTrigger
    {
        public Task<TimeSpan> GetNextDelayAsync(ScheduledContext context, CancellationToken cancellationToken)
            => Task.FromResult(TimeSpan.Zero);
    }

    private sealed class FailingTrigger : IScheduleTrigger
    {
        public Task<TimeSpan> GetNextDelayAsync(ScheduledContext context, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Trigger boom");
    }
}
