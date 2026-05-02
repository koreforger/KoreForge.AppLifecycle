using KoreForge.AppLifecycle.Events;
using KoreForge.AppLifecycle.Hosting;
using KoreForge.AppLifecycle.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KoreForge.AppLifecycle;

/// <summary>
/// DI registration helpers for the lifecycle manager.
/// </summary>
public static class ApplicationLifecycleServiceCollectionExtensions
{
    /// <summary>
    /// Adds the lifecycle manager services and hosted services to the container.
    /// </summary>
    public static IServiceCollection AddApplicationLifecycleManager(
        this IServiceCollection services,
        Action<ApplicationLifecycleOptions> configure)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new ApplicationLifecycleOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<ApplicationLifecycleEvents>();
        services.AddSingleton<IApplicationLifecycleEvents>(sp => sp.GetRequiredService<ApplicationLifecycleEvents>());

        services.AddSingleton<IHostedService, ApplicationLifecycleHostedService>();
        services.AddSingleton<IHostedService, ScheduledTasksHostedService>();

        return services;
    }
}

/// <summary>
/// Application builder helpers for the lifecycle manager.
/// </summary>
public static class ApplicationLifecycleApplicationBuilderExtensions
{
    /// <summary>
    /// Adds clarity to the ASP.NET Core pipeline; currently a no-op.
    /// </summary>
    public static IApplicationBuilder UseApplicationLifecycleManager(this IApplicationBuilder app)
    {
        if (app is null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app;
    }
}
