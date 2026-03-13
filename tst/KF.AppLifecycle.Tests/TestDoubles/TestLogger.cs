using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace KoreForge.AppLifecycle.Tests.TestDoubles;

internal sealed class TestLogger<T> : ILogger<T>
{
    private static readonly IDisposable NullScope = new DisposableScope();
    private readonly List<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        var message = formatter(state, exception);
        _entries.Add(new LogEntry(logLevel, message, exception));
    }

    public readonly record struct LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class DisposableScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
