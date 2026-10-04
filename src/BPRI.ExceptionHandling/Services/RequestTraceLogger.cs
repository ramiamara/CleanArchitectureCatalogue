using System.Data;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace BPRI.ExceptionHandling;

/// <summary>Logger Serilog dédié à la trace des requêtes (entrées et réponses). Fichier et/ou SQL Server, séparés des logs d'exceptions.</summary>
internal sealed class RequestTraceLogger : IDisposable
{
    private const string FileTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Cprj} {TraceId} {UserName} {HttpMethod:l} {Path:l}{QueryString:l} -> {StatusCode} ({DurationMs} ms){NewLine}  IN : {RequestBody:l}{NewLine}  OUT: {ResponseBody:l}{NewLine}";

    private readonly Logger _logger;

    public RequestTraceLogger(ExceptionHandlingOptions options, string contentRoot, ILogEventEnricher contextEnricher)
    {
        RequestTracingOptions tracing = options.RequestTracing;

        LoggerConfiguration config = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.With(contextEnricher);

        if (tracing.File.Enabled)
        {
            AddFileSink(config, tracing.File, contentRoot);
        }
        if (tracing.Database.Enabled)
        {
            AddDatabaseSink(config, tracing.Database, options.Database);
        }

        _logger = config.CreateLogger();
    }

    public void Write(string method, string path, string? query, int statusCode, int durationMs, string? requestBody, string? responseBody)
    {
        var properties = new ILogEventEnricher[]
        {
            new PropertyEnricher("QueryString", query ?? ""),
            new PropertyEnricher("StatusCode", statusCode),
            new PropertyEnricher("DurationMs", durationMs),
            new PropertyEnricher("RequestBody", requestBody ?? ""),
            new PropertyEnricher("ResponseBody", responseBody ?? ""),
            new PropertyEnricher("UserName", "anonymous"),
        };

        LogEventLevel level = statusCode >= 500 ? LogEventLevel.Error : statusCode >= 400 ? LogEventLevel.Warning : LogEventLevel.Information;
        _logger.ForContext(properties).Write(level, "{HttpMethod:l} {Path:l} -> {StatusCode}", method, path, statusCode);
    }

    public void Dispose()
    {
        _logger.Dispose();
    }

    private static void AddFileSink(LoggerConfiguration config, FileLogOptions file, string contentRoot)
    {
        string path = file.Path;
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(contentRoot, path);
        }

        if (file.Json)
        {
            config.WriteTo.File(ExceptionLogger.JsonFormat(file), path, rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: file.RetainedFileCount, shared: true);
        }
        else
        {
            config.WriteTo.File(path, outputTemplate: FileTemplate, rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: file.RetainedFileCount, shared: true);
        }
    }

    private static void AddDatabaseSink(LoggerConfiguration config, RequestTraceDatabaseOptions table, DatabaseLogOptions connection)
    {
        if (connection.CreateDatabaseIfMissing)
        {
            ExceptionLogger.CreateDatabaseIfMissing(connection.ConnectionString!);
        }

        var sinkOptions = new MSSqlServerSinkOptions
        {
            TableName = table.TableName,
            SchemaName = table.SchemaName,
            AutoCreateSqlTable = table.AutoCreateTable,
            BatchPostingLimit = 50,
            BatchPeriod = TimeSpan.FromSeconds(2),
        };

        config.WriteTo.MSSqlServer(
            connectionString: connection.ConnectionString!,
            sinkOptions: sinkOptions,
            columnOptions: BuildColumns());
    }

    private static ColumnOptions BuildColumns()
    {
        var columns = new ColumnOptions();
        columns.Store.Remove(StandardColumn.Properties);
        columns.Store.Remove(StandardColumn.MessageTemplate);
        columns.Store.Remove(StandardColumn.Exception);
        columns.TimeStamp.ConvertToUtc = true;

        columns.AdditionalColumns = new List<SqlColumn>
        {
            ExceptionLogger.TextColumn("TraceId", 64),
            ExceptionLogger.TextColumn("Cprj", 64),
            ExceptionLogger.TextColumn("ApplicationName", 128),
            ExceptionLogger.TextColumn("EnvironmentName", 64),
            ExceptionLogger.TextColumn("MachineName", 128),
            ExceptionLogger.TextColumn("HttpMethod", 16),
            ExceptionLogger.TextColumn("Path", 512),
            ExceptionLogger.TextColumn("QueryString", 2048),
            new SqlColumn { ColumnName = "StatusCode", DataType = SqlDbType.Int, AllowNull = true },
            new SqlColumn { ColumnName = "DurationMs", DataType = SqlDbType.Int, AllowNull = true },
            ExceptionLogger.TextColumn("UserName", 256),
            ExceptionLogger.TextColumn("UserId", 128),
            ExceptionLogger.TextColumn("RequestBody", -1),
            ExceptionLogger.TextColumn("ResponseBody", -1),
        };

        return columns;
    }
}
