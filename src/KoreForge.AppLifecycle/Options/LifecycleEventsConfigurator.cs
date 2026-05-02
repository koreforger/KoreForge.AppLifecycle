using KoreForge.AppLifecycle.Events;

namespace KoreForge.AppLifecycle.Options;

/// <summary>
/// Collects event subscriptions configured by the consumer.
/// </summary>
public sealed class LifecycleEventsConfigurator
{
    private readonly List<Action<IApplicationLifecycleEvents>> _registrations = new();

    internal IEnumerable<Action<IApplicationLifecycleEvents>> Registrations => _registrations;

    /// <summary>
    /// Registers an action that subscribes to lifecycle events.
    /// </summary>
    public LifecycleEventsConfigurator Configure(Action<IApplicationLifecycleEvents> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        _registrations.Add(configure);
        return this;
    }
}
