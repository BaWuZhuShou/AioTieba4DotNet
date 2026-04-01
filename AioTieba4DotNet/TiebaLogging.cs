using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AioTieba4DotNet;

public static class TiebaLogging
{
    private static readonly object SyncRoot = new();
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    public static ILoggerFactory Factory
    {
        get
        {
            lock (SyncRoot)
                return _factory;
        }
    }

    public static ILogger GetLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        return Factory.CreateLogger(categoryName);
    }

    public static ILogger<TCategoryName> GetLogger<TCategoryName>() => Factory.CreateLogger<TCategoryName>();

    public static ILoggerFactory EnableFileLog(string filePath, LogLevel minimumLevel = LogLevel.Information)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        lock (SyncRoot)
        {
            _factory.Dispose();
            _factory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(minimumLevel);
                builder.AddProvider(new FileLoggerProvider(filePath, minimumLevel));
            });

            return _factory;
        }
    }

    public static void Reset()
    {
        lock (SyncRoot)
        {
            _factory.Dispose();
            _factory = NullLoggerFactory.Instance;
        }
    }

    private sealed class FileLoggerProvider(string filePath, LogLevel minimumLevel) : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.Ordinal);

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new FileLogger(filePath, name, minimumLevel));

        public void Dispose()
        {
        }
    }

    private sealed class FileLogger(string filePath, string categoryName, LogLevel minimumLevel) : ILogger
    {
        private static readonly object FileLock = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var line = new StringBuilder()
                .Append(DateTimeOffset.UtcNow.ToString("O"))
                .Append(' ')
                .Append('[').Append(logLevel).Append(']')
                .Append(' ')
                .Append(categoryName)
                .Append(": ")
                .Append(message);

            if (exception is not null)
                line.Append(" | ").Append(exception.GetType().Name).Append(": ").Append(exception.Message);

            lock (FileLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
                File.AppendAllText(filePath, line.AppendLine().ToString(), Encoding.UTF8);
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
