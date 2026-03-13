# KoreForge.AppLifecycle

Opinionated lifecycle manager for ASP.NET Core and generic host applications.

## Features

- Startup and shutdown flows that wrap the host lifecycle
- Outcome-driven flow engine shared by startup, shutdown, and scheduled flows
- Lightweight in-process scheduler with conditional flows and optional overlap guards
- Async lifecycle events for visibility and telemetry
- NuGet content files that scaffold `AppLifecycle/*` folders in consuming projects

## Concepts

### Flows & Steps
A **Flow** is a directed graph of steps executed in order. The transition from one step to the next is determined by the **Outcome** of the previous step.

A **Step** is a class implementing `IFlowStep<TContext>`. It performs a single task and returns a `FlowOutcome` (e.g., `Success`, `Failure`, or a custom name like `Retry`).

### Contexts
Steps operate on a specific context, which allows passing data between steps in the same flow:
- `StartupContext`: Available during application startup.
- `ShutdownContext`: Available during application shutdown.
- `ScheduledContext`: Available during scheduled job execution.

### Triggers
Triggers determine *when* a scheduled flow runs. A trigger implements `IScheduleTrigger` and simply calculates the `TimeSpan` delay until the next execution.

## Getting Started

### 1. Simple Configuration

In your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationLifecycleManager(options =>
{
    // A simple linear startup flow
    options.Startup.Flow("Warmup")
        .BeginWith<WarmupServicesStep>()
        .EndFlow();

    // A simple scheduled task
    options.Scheduled.Flow("HealthCheck")
        .OnSchedule<EvergreenTrigger>() // Runs repeatedly based on trigger
        .BeginWith<HealthProbeStep>()
        .EndFlow();
});

// Register your steps and triggers
builder.Services.AddTransient<WarmupServicesStep>();
builder.Services.AddTransient<HealthProbeStep>();
builder.Services.AddTransient<EvergreenTrigger>();

var app = builder.Build();
app.UseApplicationLifecycleManager();
app.Run();
```

### 2. Complex Flows (Branching & Logic)

You can define complex logic by branching based on outcomes. This is useful for "Circuit Breaker" patterns or optional initialization.

```csharp
options.Startup.Flow("DatabaseInit")
    .BeginWith<CheckDatabaseConnectionStep>()
        .If(FlowOutcome.Success).Then<RunMigrationsStep>()
        .If(FlowOutcome.Failure).Then<LogCriticalErrorStep>()
        
    // Chaining from RunMigrationsStep (Success is implicit if not specified)
    .Then<SeedReferenceDataStep>()
    .EndFlow();
```

### 3. Implementing a Step

Steps focus on a single responsibility. They receive the flow context and a cancellation token.

```csharp
public class CheckDatabaseConnectionStep : IFlowStep<StartupContext>
{
    private readonly IDbConnectionFactory _db;

    public CheckDatabaseConnectionStep(IDbConnectionFactory db) => _db = db;

    public async Task<FlowOutcome> ExecuteAsync(StartupContext context, CancellationToken ct)
    {
        if (await _db.IsAvailableAsync(ct))
        {
            return FlowOutcome.Success; 
        }

        // Custom outcomes can drive specific branches
        // return FlowOutcome.Custom("Timeout"); 
        
        return FlowOutcome.Failure;
    }
}
```

### 4. Implementing a Schedule Trigger

Triggers control the timing of scheduled flows. They are purely time-calculators.

```csharp
public class BusinessHoursTrigger : IScheduleTrigger
{
    public Task<TimeSpan> GetNextDelayAsync(ScheduledContext context, CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        
        // If it's outside 9-5, wait until tomorrow at 9 AM
        if (now.Hour >= 17 || now.Hour < 9)
        {
             var tomorrow9am = now.Date.AddDays(1).AddHours(9);
             if (now.Hour < 9) tomorrow9am = now.Date.AddHours(9);
             
             return Task.FromResult(tomorrow9am - now);
        }

        // Otherwise run every 15 minutes
        return Task.FromResult(TimeSpan.FromMinutes(15));
    }
}
```

## Repository Layout

- `src/KoreForge.AppLifecycle` – library implementation
- `tests/KoreForge.AppLifecycle.Tests` – xUnit test suite for engine, DSL, scheduler, and integration scenarios
- `samples/KoreForge.AppLifecycle.SampleWebApp` – minimal ASP.NET Core app showcasing startup/shutdown/scheduled flows
- `docs/Specification.md` – high-level design document

## Documentation

- [Specification](docs/Specification.md) – product goals, architecture, and flow diagrams
- [Usage Guide](docs/UsageGuide.md) – walkthrough for configuring flows, triggers, and events
- [Development Guide](docs/DevelopmentGuide.md) – conventions, tooling, and local workflows
- [Build & Release](docs/BuildAndRelease.md) – build, test, coverage, packaging, and publishing steps
- [Versioning Guide](docs/versioning-guide.md) – semantic versioning rules and MinVer tag strategy

## License

MIT
