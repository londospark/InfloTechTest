using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace UserManagement.Web.Tests.TestHelpers;

/// <summary>
/// Lightweight test logger that captures log messages for verification
/// </summary>
public sealed class MockLogger<T> : ILogger, ILogger<T>
{
    private readonly System.Collections.Concurrent.ConcurrentBag<LogEntry> _entries = [];

    public record LogEntry(LogLevel Level, string Message, Exception? Exception = null);

    public ILogger<T> AsILogger() => this;

    public bool LogContains(LogLevel level, string contains) =>
        _entries.Any(e => e.Level == level && e.Message.Contains(contains, StringComparison.OrdinalIgnoreCase));

    public bool AnyLogContains(string contains) =>
        _entries.Any(e => e.Message.Contains(contains, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<LogEntry> GetLogs() => [.. _entries];

    public IEnumerable<LogEntry> GetLogs(LogLevel level) =>
        [.. _entries.Where(e => e.Level == level)];

    public void Clear() => _entries.Clear();

    internal void Add(LogLevel level, string message, Exception? exception = null) =>
        _entries.Add(new LogEntry(level, message, exception));

    public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
        NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) => Add(logLevel, formatter(state, exception), exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
