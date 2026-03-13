using System.Linq;
using Microsoft.Extensions.Logging;

namespace KoreForge.AppLifecycle.Internal;

internal static class EventDispatcher
{
    public static async Task InvokeAsync<TArgs>(Func<TArgs, Task>? handler, TArgs args, ILogger logger, string eventName)
    {
        if (handler is null)
        {
            return;
        }

        foreach (var subscriber in handler.GetInvocationList().Cast<Func<TArgs, Task>>())
        {
            try
            {
                await subscriber(args).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lifecycle event {EventName} threw an exception.", eventName);
            }
        }
    }
}
