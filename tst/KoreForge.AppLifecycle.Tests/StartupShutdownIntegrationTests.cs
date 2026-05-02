using KoreForge.AppLifecycle.Events;
using KoreForge.AppLifecycle.Flows;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Options;
using KoreForge.AppLifecycle.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KoreForge.AppLifecycle.Tests;

public sealed class StartupShutdownIntegrationTests
{
    [Fact]
    public async Task StartAsync_FailFastOnFailureThrows()
    {
        using var provider = BuildServices(services => services.AddTransient<FailingStartupStep>());
        var options = new ApplicationLifecycleOptions
        {
            FailFastOnStartupFailure = true
        };

        options.Startup.Flow("Fail")
            .BeginWith<FailingStartupStep>()
            .EndFlow();

        var service = CreateHostedService(provider, options);

        await Assert.ThrowsAsync<ApplicationLifecycleException>(() => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_WhenFailFastDisabled_Completes()
    {
        using var provider = BuildServices(services => services.AddTransient<FailingShutdownStep>());
        var options = new ApplicationLifecycleOptions
        {
            FailFastOnShutdownFailure = false
        };

        options.Shutdown.Flow("Fail")
            .BeginWith<FailingShutdownStep>()
            .EndFlow();

        var service = CreateHostedService(provider, options);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_FailFastEnabledThrowsOnFailure()
    {
        using var provider = BuildServices(services => services.AddTransient<FailingShutdownStep>());
        var options = new ApplicationLifecycleOptions
        {
            FailFastOnShutdownFailure = true
        };

        options.Shutdown.Flow("Fail")
            .BeginWith<FailingShutdownStep>()
            .EndFlow();

        var service = CreateHostedService(provider, options);

        await Assert.ThrowsAsync<ApplicationLifecycleException>(() => service.StopAsync(CancellationToken.None));
    }

    private static ApplicationLifecycleHostedService CreateHostedService(ServiceProvider provider, ApplicationLifecycleOptions options)
    {
        var logger = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning)).CreateLogger<ApplicationLifecycleHostedService>();
        return new ApplicationLifecycleHostedService(provider, new TestHostEnvironment(), options, new ApplicationLifecycleEvents(options), logger);
    }

    private static ServiceProvider BuildServices(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private sealed class FailingStartupStep : IFlowStep<StartupContext>
    {
        public Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken cancellationToken)
            => Task.FromResult(FlowOutcome.Failure);
    }

    private sealed class FailingShutdownStep : IFlowStep<ShutdownContext>
    {
        public Task<FlowOutcome> ExecuteAsync(ShutdownContext context, CancellationToken cancellationToken)
            => Task.FromResult(FlowOutcome.Failure);
    }
}
