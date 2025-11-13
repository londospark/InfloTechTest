using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;

namespace UserManagement.Web.Helpers;

/// <summary>
/// Decorates the default logger pipeline and persists important log messages to the database.
/// </summary>
/// <typeparam name="T">The category type for the logger.</typeparam>
public sealed class DatabaseLogger<T>(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, ILogger? forwardLogger = null) : ILogger<T>
{
	private static readonly HashSet<LogLevel> PersistedLevels = new()
	{
		LogLevel.Information,
		LogLevel.Warning,
		LogLevel.Error,
		LogLevel.Critical
	};

	private readonly ILogger _innerLogger = loggerFactory.CreateLogger(typeof(T));
	private readonly ILogger? _forwardLogger = forwardLogger;
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
	private readonly AsyncLocal<long?> _currentUserId = new();

	/// <inheritdoc />
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		var innerScope = _innerLogger.BeginScope(state);
		var previous = _currentUserId.Value;
		var next = ExtractUserId(state);
		if (!next.HasValue)
		{
			return innerScope;
		}

		_currentUserId.Value = next.Value;
		return new Scope(() =>
		{
			_currentUserId.Value = previous;
			innerScope?.Dispose();
		});
	}

	/// <inheritdoc />
	public bool IsEnabled(LogLevel logLevel) => _innerLogger.IsEnabled(logLevel);

	/// <inheritdoc />
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!_innerLogger.IsEnabled(logLevel))
		{
			return;
		}

		// Build message early because we'll use it either for persisting or forwarding
		var message = formatter(state, exception);
		if (exception is not null)
		{
			message = $"{message} | Exception: {exception.Message}";
		}

		if (string.IsNullOrWhiteSpace(message))
		{
			// Nothing useful to log/forward
			return;
		}

		// Should this entry be persisted?
		if (!PersistedLevels.Contains(logLevel))
		{
			// Not a persisted level -> forward
			var forward = _forwardLogger ?? _innerLogger;
			forward.Log(logLevel, eventId, state, exception, formatter);
			return;
		}

		// Try get user id from current scope or state
		var userIdOpt = _currentUserId.Value ?? ExtractUserId(state);
		if (!userIdOpt.HasValue)
		{
			// No user id present - forward instead of persisting
			var forward = _forwardLogger ?? _innerLogger;
			forward.Log(logLevel, eventId, state, exception, formatter);
			return;
		}

		// Persisted: log to inner logger then store in DB
		_innerLogger.Log(logLevel, eventId, state, exception, formatter);

		// Resolve IUserLogService from a new scope to avoid depending on scoped services in constructor
		using var scope = _scopeFactory.CreateScope();
		var userLogService = scope.ServiceProvider.GetRequiredService<IUserLogService>();

		var log = new UserLog
		{
			UserId = userIdOpt.Value,
			Message = message,
			CreatedAt = DateTime.UtcNow
		};

		userLogService.Add(log);
	}

	private static long? ExtractUserId<TState>(TState state)
	{
		if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
		{
			var matched = kvps.FirstOrDefault(pair => string.Equals(pair.Key, "UserId", StringComparison.OrdinalIgnoreCase));
			if (!EqualityComparer<KeyValuePair<string, object?>>.Default.Equals(matched, default))
			{
				if (matched.Value is long longValue)
				{
					return longValue;
				}

				if (matched.Value is int intValue)
				{
					return intValue;
				}
			}
		}

		return null;
	}

	private sealed class Scope(Action onDispose) : IDisposable
	{
		private readonly Action _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
		private int _disposed;
		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 0)
			{
				_onDispose();
			}
		}
	}
}
