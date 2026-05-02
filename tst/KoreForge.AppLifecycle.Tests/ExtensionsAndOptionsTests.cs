using KoreForge.AppLifecycle.Events;
using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Options;
using KoreForge.AppLifecycle.Scheduling;
using KoreForge.AppLifecycle.Tests.TestDoubles;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KoreForge.AppLifecycle.Tests;

public sealed class ExtensionsAndOptionsTests
{
    // ------------------------------------------------------------------
    // AddApplicationLifecycleManager
    // ------------------------------------------------------------------

    [Fact]
    public void AddApplicationLifecycleManager_RegistersRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        services.AddApplicationLifecycleManager(options =>
        {
            options.Startup.Flow("Boot").BeginWith<StubStartupStep>().EndFlow();
        });
        services.AddTransient<StubStartupStep>();

        using var provider = services.BuildServiceProvider();

        var opts = provider.GetService<ApplicationLifecycleOptions>();
        Assert.NotNull(opts);

        var events = provider.GetService<ApplicationLifecycleEvents>();
        Assert.NotNull(events);

        var eventsInterface = provider.GetService<IApplicationLifecycleEvents>();
        Assert.NotNull(eventsInterface);
        Assert.Same(events, eventsInterface);
    }

    [Fact]
    public void AddApplicationLifecycleManager_NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationLifecycleServiceCollectionExtensions.AddApplicationLifecycleManager(
                null!, _ => { }));
    }

    [Fact]
    public void AddApplicationLifecycleManager_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddApplicationLifecycleManager(null!));
    }

    [Fact]
    public void UseApplicationLifecycleManager_ReturnsApp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var returned = app.UseApplicationLifecycleManager();
        Assert.Same(app, returned);
    }

    [Fact]
    public void UseApplicationLifecycleManager_NullApp_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationLifecycleApplicationBuilderExtensions.UseApplicationLifecycleManager(null!));
    }

    // ------------------------------------------------------------------
    // FlowOutcome value semantics
    // ------------------------------------------------------------------

    [Fact]
    public void FlowOutcome_EqualityByName()
    {
        var a = new FlowOutcome("Done");
        var b = new FlowOutcome("Done");
        var c = new FlowOutcome("Other");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
    }

    [Fact]
    public void FlowOutcome_EqualsObject_BoxedValue()
    {
        object o = FlowOutcome.Success;
        Assert.True(FlowOutcome.Success.Equals(o));
        Assert.False(FlowOutcome.Success.Equals("not an outcome"));
    }

    [Fact]
    public void FlowOutcome_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FlowOutcome(""));
        Assert.Throws<ArgumentException>(() => new FlowOutcome("   "));
    }

    [Fact]
    public void FlowOutcome_ToString_ReturnsName()
    {
        Assert.Equal("Success", FlowOutcome.Success.ToString());
        Assert.Equal("Custom", FlowOutcome.Custom("Custom").ToString());
    }

    // ------------------------------------------------------------------
    // ApplicationLifecycleException
    // ------------------------------------------------------------------

    [Fact]
    public void ApplicationLifecycleException_MessageOnly()
    {
        var ex = new ApplicationLifecycleException("test message");
        Assert.Equal("test message", ex.Message);
    }

    [Fact]
    public void ApplicationLifecycleException_WithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new ApplicationLifecycleException("outer", inner);
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    // ------------------------------------------------------------------
    // ApplicationLifecycleOptions defaults
    // ------------------------------------------------------------------

    [Fact]
    public void ApplicationLifecycleOptions_DefaultsAreExpected()
    {
        var options = new ApplicationLifecycleOptions();
        Assert.True(options.FailFastOnStartupFailure);
        Assert.False(options.FailFastOnShutdownFailure);
        Assert.Equal(UnmappedOutcomePolicy.StopFlow, options.UnmappedOutcomePolicy);
        Assert.True(options.LogStepExceptions);
        Assert.False(options.LogUnmappedOutcomes);
    }

    // ------------------------------------------------------------------
    // BuildStartupFlows / BuildShutdownFlows / BuildScheduledFlows
    // ------------------------------------------------------------------

    [Fact]
    public void BuildStartupFlows_ReturnsRegisteredFlows()
    {
        var options = new ApplicationLifecycleOptions();
        options.Startup.Flow("Init").BeginWith<StubStartupStep>().EndFlow();
        options.Startup.Flow("Warm").BeginWith<StubStartupStep2>().EndFlow();

        var flows = options.BuildStartupFlows();
        Assert.Equal(2, flows.Count);
        Assert.Equal("Init", flows[0].Name);
        Assert.Equal("Warm", flows[1].Name);
    }

    [Fact]
    public void BuildShutdownFlows_ReturnsRegisteredFlows()
    {
        var options = new ApplicationLifecycleOptions();
        options.Shutdown.Flow("Drain").BeginWith<StubShutdownStep>().EndFlow();

        var flows = options.BuildShutdownFlows();
        Assert.Single(flows);
        Assert.Equal("Drain", flows[0].Name);
    }

    [Fact]
    public void BuildScheduledFlows_ReturnsRegisteredFlows()
    {
        var options = new ApplicationLifecycleOptions();
        options.Scheduled.Flow("Scan")
            .OnSchedule<StubTrigger>()
            .BeginWith<StubScheduledStep>()
            .EndFlow();

        var flows = options.BuildScheduledFlows();
        Assert.Single(flows);
        Assert.Equal("Scan", flows[0].FlowName);
    }

    // ------------------------------------------------------------------
    // ScheduledFlow NoOverlap
    // ------------------------------------------------------------------

    [Fact]
    public void ScheduledFlow_NoOverlap_SetsProperly()
    {
        var options = new ApplicationLifecycleOptions();
        options.Scheduled.Flow("Guarded")
            .OnSchedule<StubTrigger>()
            .NoOverlap()
            .BeginWith<StubScheduledStep>()
            .EndFlow();

        var flows = options.BuildScheduledFlows();
        Assert.Single(flows);
        Assert.True(flows[0].NoOverlap);
    }

    // ------------------------------------------------------------------
    // Context constructors
    // ------------------------------------------------------------------

    [Fact]
    public void StartupContext_NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new StartupContext(null!, new TestHostEnvironment()));
    }

    [Fact]
    public void StartupContext_NullHostEnvironment_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new StartupContext(new ServiceCollection().BuildServiceProvider(), null!));
    }

    [Fact]
    public void ShutdownContext_NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ShutdownContext(null!, new TestHostEnvironment()));
    }

    [Fact]
    public void ScheduledContext_NullFlowName_Throws()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        Assert.Throws<ArgumentNullException>(() =>
            new ScheduledContext(provider, new TestHostEnvironment(), null!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ScheduledContext_Properties_AreSet()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var env = new TestHostEnvironment();
        var now = DateTimeOffset.UtcNow;
        var ctx = new ScheduledContext(provider, env, "MyFlow", now);

        Assert.Same(provider, ctx.Services);
        Assert.Same(env, ctx.HostEnvironment);
        Assert.Equal("MyFlow", ctx.FlowName);
        Assert.Equal(now, ctx.ScheduledTime);
    }

    // ------------------------------------------------------------------
    // Step helpers
    // ------------------------------------------------------------------

    private sealed class StubStartupStep : IFlowStep<StartupContext>
    {
        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken ct)
            => Task.FromResult(FlowOutcome.Success);
    }

    private sealed class StubStartupStep2 : IFlowStep<StartupContext>
    {
        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken ct)
            => Task.FromResult(FlowOutcome.Success);
    }

    private sealed class StubShutdownStep : IFlowStep<ShutdownContext>
    {
        public Task<FlowOutcome> ExecuteAsync(ShutdownContext context, CancellationToken ct)
            => Task.FromResult(FlowOutcome.Success);
    }

    private sealed class StubScheduledStep : IFlowStep<ScheduledContext>
    {
        public Task<FlowOutcome> ExecuteAsync(ScheduledContext context, CancellationToken ct)
            => Task.FromResult(FlowOutcome.Success);
    }

    private sealed class StubTrigger : IScheduleTrigger
    {
        public Task<TimeSpan> GetNextDelayAsync(ScheduledContext context, CancellationToken ct)
            => Task.FromResult(TimeSpan.FromMinutes(1));
    }
}
