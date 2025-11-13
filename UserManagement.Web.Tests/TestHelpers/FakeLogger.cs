using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace UserManagement.Web.Tests.TestHelpers;

/// <summary>
/// Fake logger factory and logger for testing scenarios
/// </summary>
public sealed class FakeLoggerFactory : ILoggerFactory
{
    private readonly FakeLogger _logger = new();

    public ILogger CreateLogger(string categoryName) => _logger;

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }

    public FakeLogger GetLogger() => _logger;
}

/// <summary>
/// Fake logger that tracks whether logging occurred
/// </summary>
public sealed class FakeLogger : ILogger
{
    public bool Logged { get; private set; }
    public LogLevel? LastLevel { get; private set; }
    public object? LastState { get; private set; }
    public Exception? LastException { get; private set; }
    public List<(LogLevel Level, string Message)> Messages { get; } = [];

    public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
        NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Logged = true;
        LastLevel = logLevel;
        LastState = state;
        LastException = exception;
        Messages.Add((logLevel, formatter(state, exception)));
    }

    public void Reset()
    {
        Logged = false;
        LastLevel = null;
        LastState = null;
        LastException = null;
        Messages.Clear();
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
