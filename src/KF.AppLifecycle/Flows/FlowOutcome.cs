namespace KoreForge.AppLifecycle.Flows;

/// <summary>
/// Represents the outcome of executing a flow step.
/// </summary>
public readonly struct FlowOutcome : IEquatable<FlowOutcome>
{
    private static readonly StringComparer Comparer = StringComparer.Ordinal;

    public string Name { get; }

    public static FlowOutcome Success { get; } = new("Success");
    public static FlowOutcome Failure { get; } = new("Failure");

    public FlowOutcome(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Outcome name cannot be null or whitespace.", nameof(name));
        }

        Name = name;
    }

    public static FlowOutcome Custom(string name) => new(name);

    public bool Equals(FlowOutcome other) => Comparer.Equals(Name, other.Name);

    public override bool Equals(object? obj) => obj is FlowOutcome other && Equals(other);

    public override int GetHashCode() => Comparer.GetHashCode(Name);

    public static bool operator ==(FlowOutcome left, FlowOutcome right) => left.Equals(right);

    public static bool operator !=(FlowOutcome left, FlowOutcome right) => !left.Equals(right);

    public override string ToString() => Name;
}
