namespace KoreForge.AppLifecycle.Options;

/// <summary>
/// Determines how the engine should behave when a step returns an unmapped outcome.
/// </summary>
public enum UnmappedOutcomePolicy
{
    /// <summary>Terminate the flow gracefully when an unmapped outcome is returned.</summary>
    StopFlow = 0,

    /// <summary>Treat the unmapped outcome as a failure and use the failure transition.</summary>
    TreatAsFailure = 1,

    /// <summary>Throw an <see cref="ApplicationLifecycleException"/>.</summary>
    Throw = 2
}
