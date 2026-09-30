using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace BPRI.ExceptionHandling;

/// <summary>Envoie les logs ILogger de l'application vers le logger Serilog (donc vers la base), avec le même contexte que les exceptions.</summary>
internal sealed class ApplicationLogProvider : ILoggerProvider
{
    private readonly ExceptionLogger _logger;
    private readonly ApplicationLogsOptions _options;

    public ApplicationLogProvider(ExceptionLogger logger, ApplicationLogsOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public ILogger CreateLogger(string categoryName)
    {
        bool isFramework = categoryName.StartsWith("Microsoft.", StringComparison.Ordinal)
            || categoryName.StartsWith("System.", StringComparison.Ordinal);

        LogLevel minimum = _options.MinimumLevel;
        if (isFramework)
        {
            minimum = _options.FrameworkMinimumLevel;
        }

        return new Bridge(_logger.ForCategory(categoryName), minimum);
    }

    public void Dispose()
    {
    }

    private sealed class Bridge : ILogger
    {
        private readonly Serilog.ILogger _target;
        private readonly LogLevel _minimum;

        public Bridge(Serilog.ILogger target, LogLevel minimum)
        {
            _target = target;
            _minimum = minimum;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= _minimum;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);
            _target.Write(ToSerilogLevel(logLevel), exception, "{AppMessage:l}", message);
        }

        private static LogEventLevel ToSerilogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return LogEventLevel.Verbose;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                default:
                    return LogEventLevel.Fatal;
            }
        }
    }
}
