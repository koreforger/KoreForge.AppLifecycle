namespace KoreForge.AppLifecycle;

/// <summary>
/// Represents errors that occur while configuring or executing application lifecycle flows.
/// </summary>
public sealed class ApplicationLifecycleException : Exception
{
    public ApplicationLifecycleException(string message)
        : base(message)
    {
    }

    public ApplicationLifecycleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
