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

public sealed class EventsTests
{
    [Fact]
    public async Task StartupEvents_FireInOrder()
    {
        var calls = new List<string>();
        var options = new ApplicationLifecycleOptions();
        options.Events.Configure(events =>
        {
            events.BeforeStartupFlows += args => Record("before");
            events.StartupStepExecuting += args => Record("executing");
            events.StartupStepExecuted += args => Record("executed");
            events.AfterStartupFlows += args => Record("after");

            Task Record(string name)
            {
                calls.Add(name);
                return Task.CompletedTask;
            }
        });

        options.Startup.Flow("Events")
            .BeginWith<SuccessStep>()
            .EndFlow();

        using var provider = BuildServices(services => services.AddTransient<SuccessStep>());
        var service = CreateHostedService(provider, options);

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(new[] { "before", "executing", "executed", "after" }, calls);
    }

    [Fact]
    public async Task StartupEvents_HandlerExceptionIsLoggedAndIgnored()
    {
        var options = new ApplicationLifecycleOptions();
        options.Events.Configure(events =>
        {
            events.BeforeStartupFlows += _ => throw new InvalidOperationException("boom");
        });

        options.Startup.Flow("Events")
            .BeginWith<SuccessStep>()
            .EndFlow();

        using var provider = BuildServices(services => services.AddTransient<SuccessStep>());
        var logger = new TestLogger<ApplicationLifecycleHostedService>();
        var service = new ApplicationLifecycleHostedService(provider, new TestHostEnvironment(), options, new ApplicationLifecycleEvents(options), logger);

        await service.StartAsync(CancellationToken.None);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("BeforeStartupFlows"));
    }

    private static ApplicationLifecycleHostedService CreateHostedService(ServiceProvider provider, ApplicationLifecycleOptions options)
    {
        var logger = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug)).CreateLogger<ApplicationLifecycleHostedService>();
        return new ApplicationLifecycleHostedService(provider, new TestHostEnvironment(), options, new ApplicationLifecycleEvents(options), logger);
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
}
