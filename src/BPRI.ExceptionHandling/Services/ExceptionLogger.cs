using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;
using Serilog.Sinks.Email;
using Serilog.Sinks.MSSqlServer;

namespace BPRI.ExceptionHandling;

/// <summary>
/// Logger Serilog unique : exceptions (fichier, SQL Server, e-mail) et logs ILogger de l'application (SQL Server uniquement).
/// Indépendant du logger de l'application.
/// </summary>
internal sealed class ExceptionLogger : IDisposable
{
    private const string FileTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Cprj} {TraceId} {SourceContext} {StatusCode} {ExceptionType}: {Message:lj}{NewLine}{Exception}";

    private readonly Logger _logger;

    public ExceptionLogger(ExceptionHandlingOptions options, string contentRoot, ILogEventEnricher contextEnricher)
    {
        EnableSelfLog();

        LoggerConfiguration config = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.With(contextEnricher);

        if (options.File.Enabled)
        {
            AddFileSink(config, options.File, contentRoot);
        }
        if (options.Database.Enabled)
        {
            AddDatabaseSink(config, options.Database);
        }
        if (options.Email.Enabled)
        {
            AddEmailSink(config, options.Email);
        }

        _logger = config.CreateLogger();
    }

    /// <summary>Logger pour une catégorie ILogger (logs de l'application).</summary>
    public Serilog.ILogger ForCategory(string category)
    {
        return _logger.ForContext("SourceContext", category);
    }

    /// <summary>Exception gérée par le middleware. Le contexte (cprj, utilisateur, TraceId…) est ajouté par l'enricher.</summary>
    public void Write(LogEventLevel level, Exception exception, int statusCode)
    {
        var properties = new ILogEventEnricher[]
        {
            new PropertyEnricher("StatusCode", statusCode),
            new PropertyEnricher("ExceptionType", exception.GetType().FullName),
            new PropertyEnricher("InnerException", exception.InnerException?.ToString()),
            new PropertyEnricher("Fingerprint", Fingerprint(exception)),
        };

        Serilog.ILogger log = _logger.ForContext(properties);
        log.Write(level, exception, "{ExceptionMessage:l}", exception.Message);
    }

    public void Dispose()
    {
        _logger.Dispose();
    }

    /// <summary>Le fichier et l'e-mail ne reçoivent que les exceptions gérées par le middleware (pas les logs ILogger).</summary>
    private static bool IsException(LogEvent logEvent)
    {
        return logEvent.Properties.ContainsKey("ExceptionType");
    }

    /// <summary>JSON complet ou JSON compact selon File:Compact.</summary>
    internal static ITextFormatter JsonFormat(FileLogOptions file)
    {
        if (file.Compact)
        {
            return new CompactJsonFormatter();
        }
        return new JsonFormatter();
    }

    private static void EnableSelfLog()
    {
        Serilog.Debugging.SelfLog.Enable(message =>
        {
            Console.Error.WriteLine("[BPRI.ExceptionHandling] " + message);
            System.Diagnostics.Debug.WriteLine("[BPRI.ExceptionHandling] " + message);
        });
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
            config.WriteTo.File(JsonFormat(file), path, rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: file.RetainedFileCount, shared: true);
        }
        else
        {
            config.WriteTo.File(path, outputTemplate: FileTemplate, rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: file.RetainedFileCount, shared: true);
        }
    }

    private static void AddDatabaseSink(LoggerConfiguration config, DatabaseLogOptions database)
    {
        if (database.CreateDatabaseIfMissing)
        {
            CreateDatabaseIfMissing(database.ConnectionString!);
        }

        var sinkOptions = new MSSqlServerSinkOptions
        {
            TableName = database.TableName,
            SchemaName = database.SchemaName,
            AutoCreateSqlTable = database.AutoCreateTable,
            BatchPostingLimit = 50,
            BatchPeriod = TimeSpan.FromSeconds(2),
        };

        config.WriteTo.MSSqlServer(
            connectionString: database.ConnectionString!,
            sinkOptions: sinkOptions,
            columnOptions: BuildColumns());
    }

    private static void AddEmailSink(LoggerConfiguration config, EmailLogOptions email)
    {
        string subjectPrefix = email.SubjectPrefix.Replace("{", "{{").Replace("}", "}}");

        NetworkCredential? credentials = null;
        if (!string.IsNullOrEmpty(email.UserName))
        {
            credentials = new NetworkCredential(email.UserName, email.Password);
        }

        var sinkOptions = new EmailSinkOptions
        {
            From = email.From,
            To = new List<string>(email.To),
            Host = email.Host,
            Port = email.Port,
            ConnectionSecurity = email.Security,
            Credentials = credentials,
            Subject = new MessageTemplateTextFormatter(subjectPrefix + "{ApplicationName:l} - {ExceptionType:l} ({StatusCode})"),
            Body = new HtmlEmailFormatter(email.TemplatePath),
            IsBodyHtml = true,
        };

        config.WriteTo.Logger(sub =>
        {
            sub.Filter.ByIncludingOnly(IsException);
            sub.WriteTo.Email(
                options: sinkOptions,
                batchingOptions: new()
                {
                    BatchSizeLimit = email.BatchSize,
                    BufferingTimeLimit = TimeSpan.FromSeconds(email.BatchPeriodSeconds),
                },
                restrictedToMinimumLevel: email.MinimumLevel);
        });
    }

    /// <summary>Colonnes standard Serilog (Id, Message, Level, TimeStamp, Exception) + colonnes de recherche.</summary>
    private static ColumnOptions BuildColumns()
    {
        var columns = new ColumnOptions();
        columns.Store.Remove(StandardColumn.Properties);
        columns.Store.Remove(StandardColumn.MessageTemplate);
        columns.TimeStamp.ConvertToUtc = true;

        columns.AdditionalColumns = new List<SqlColumn>
        {
            TextColumn("TraceId", 64),
            TextColumn("Cprj", 64),
            TextColumn("ApplicationName", 128),
            TextColumn("EnvironmentName", 64),
            TextColumn("MachineName", 128),
            TextColumn("SourceContext", 256),
            new SqlColumn { ColumnName = "StatusCode", DataType = SqlDbType.Int, AllowNull = true },
            TextColumn("ExceptionType", 256),
            TextColumn("InnerException", -1),
            TextColumn("Fingerprint", 16),
            TextColumn("HttpMethod", 16),
            TextColumn("Path", 512),
            TextColumn("UserName", 256),
            TextColumn("UserId", 128),
            TextColumn("Claims", -1),
        };

        return columns;
    }

    internal static SqlColumn TextColumn(string name, int length)
    {
        return new SqlColumn { ColumnName = name, DataType = SqlDbType.NVarChar, DataLength = length, AllowNull = true };
    }

    /// <summary>Empreinte (type + première ligne de la stack trace) pour regrouper les occurrences d'une même erreur.</summary>
    private static string Fingerprint(Exception exception)
    {
        string firstFrame = "";
        if (exception.StackTrace != null)
        {
            firstFrame = exception.StackTrace.Split('\n', 2)[0].Trim();
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(exception.GetType().FullName + "|" + firstFrame));
        return Convert.ToHexString(hash, 0, 8);
    }

    internal static void CreateDatabaseIfMissing(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string database = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(database))
            {
                return;
            }

            builder.InitialCatalog = "master";
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            using SqlCommand command = connection.CreateCommand();
            command.CommandText = "IF DB_ID(@name) IS NULL EXEC('CREATE DATABASE [' + REPLACE(@name, ']', ']]') + ']')";
            command.Parameters.AddWithValue("@name", database);
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[BPRI.ExceptionHandling] Création de la base impossible : " + ex.Message);
        }
    }
}
