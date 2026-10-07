using BPRI.LogsPlatform.Back.Contracts;
using BPRI.LogsPlatform.Back.Data;
using BPRI.LogsPlatform.Back.Errors;
using BPRI.LogsPlatform.Back.Queries;
using Microsoft.EntityFrameworkCore;

namespace BPRI.LogsPlatform.Back.Services;

public interface ILogService
{
    Task<PagedResult<LogListItemDto>> GetLogs(LogFilter filter, CancellationToken ct);
    Task<LogDetailDto> GetLog(int id, CancellationToken ct);
    Task<StatsDto> GetStats(string? project, string? application, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    Task<FiltersDto> GetFilters(string? project, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
}

/// <summary>Exceptions : liste, détail, statistiques et filtres (table dbo.ExceptionLogs).</summary>
public sealed class LogService : ILogService
{
    private static readonly DateTime Epoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly LogsDbContext _db;

    public LogService(LogsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<LogListItemDto>> GetLogs(LogFilter filter, CancellationToken ct)
    {
        string cprj = LogQueries.RequireProject(filter.Project);
        var (from, to) = LogQueries.ResolveRange(filter.From, filter.To);
        int page = Math.Max(1, filter.Page);
        int size = Math.Clamp(filter.PageSize, 1, LogQueries.MaxPageSize);

        var query = _db.InScope(cprj, from, to).ApplyFilter(filter);
        int total = await query.CountAsync(ct);

        var rows = await query.Sort(filter.SortField, filter.SortDir)
            .Skip((page - 1) * size).Take(size)
            .Select(l => new { l.Id, l.TraceId, l.Cprj, l.ApplicationName, l.Level, l.StatusCode, l.ExceptionType, l.SourceContext, l.Message, l.HttpMethod, l.Path, l.UserName, l.TimeStamp })
            .ToListAsync(ct);

        var items = rows.Select(l => new LogListItemDto(
            l.Id, l.TraceId ?? "", l.Cprj ?? "", l.ApplicationName ?? "", LogQueries.NormalizeLevel(l.Level), l.StatusCode,
            l.ExceptionType, l.SourceContext, Truncate(l.Message, 300), l.HttpMethod, l.Path, l.UserName, l.TimeStamp.AsUtc())).ToList();

        return new PagedResult<LogListItemDto>(items, total, page, size);
    }

    public async Task<LogDetailDto> GetLog(int id, CancellationToken ct)
    {
        LogEntry? l = await _db.Logs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l == null)
        {
            throw new ApiException("Log " + id + " introuvable.", 404);
        }

        return new LogDetailDto(
            l.Id, l.TraceId ?? "", l.Cprj ?? "", l.ApplicationName ?? "", LogQueries.NormalizeLevel(l.Level), l.StatusCode,
            l.ExceptionType, l.SourceContext, l.Message ?? "", l.HttpMethod, l.Path, l.UserName, l.TimeStamp.AsUtc(),
            l.UserId, l.Claims, l.Exception, l.InnerException, l.EnvironmentName, l.MachineName, l.Fingerprint);
    }

    public async Task<StatsDto> GetStats(string? project, string? application, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        string cprj = LogQueries.RequireProject(project);
        var (fromUtc, toUtc) = LogQueries.ResolveRange(from, to);
        var scope = _db.InScope(cprj, fromUtc, toUtc, application);

        var bySeverity = (await scope.GroupBy(l => l.Level).Select(g => new { Level = g.Key, Count = g.Count() }).ToListAsync(ct))
            .GroupBy(x => LogQueries.NormalizeLevel(x.Level))
            .Select(g => new SeverityCount(g.Key, g.Sum(x => x.Count))).ToList();

        var statusRows = await scope.Where(l => l.StatusCode != null)
            .GroupBy(l => l.StatusCode!.Value)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var byStatus = statusRows.OrderBy(x => x.Code).Select(x => new StatusCodeCount(x.Code, x.Count)).ToList();

        var typeRows = await scope.Where(l => l.ExceptionType != null)
            .GroupBy(l => l.ExceptionType!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var byType = typeRows.Select(x => new ExceptionTypeCount(x.Type, x.Count)).ToList();

        var endpointRows = await scope.Where(l => l.Path != null)
            .GroupBy(l => new { l.HttpMethod, l.Path })
            .OrderByDescending(g => g.Count())
            .Take(8)
            .Select(g => new { g.Key.HttpMethod, g.Key.Path, Count = g.Count() })
            .ToListAsync(ct);
        var endpoints = endpointRows.Select(x => new EndpointCount(x.HttpMethod, x.Path, x.Count)).ToList();

        bool hourly = (toUtc - fromUtc).TotalDays <= 2;
        var buckets = new List<(int Bucket, string Level, int Count)>();
        if (hourly)
        {
            var rows = await scope
                .GroupBy(l => new { Bucket = EF.Functions.DateDiffHour(Epoch, l.TimeStamp), l.Level })
                .Select(g => new { g.Key.Bucket, g.Key.Level, Count = g.Count() })
                .ToListAsync(ct);
            foreach (var row in rows)
            {
                buckets.Add((row.Bucket, LogQueries.NormalizeLevel(row.Level), row.Count));
            }
        }
        else
        {
            var rows = await scope
                .GroupBy(l => new { Bucket = EF.Functions.DateDiffDay(Epoch, l.TimeStamp), l.Level })
                .Select(g => new { g.Key.Bucket, g.Key.Level, Count = g.Count() })
                .ToListAsync(ct);
            foreach (var row in rows)
            {
                buckets.Add((row.Bucket, LogQueries.NormalizeLevel(row.Level), row.Count));
            }
        }

        var timeline = BuildTimeline(buckets, fromUtc, toUtc, hourly);

        string bucketName = "day";
        if (hourly)
        {
            bucketName = "hour";
        }

        return new StatsDto(
            bySeverity.Sum(x => x.Count), CountOf(bySeverity, "Error"), CountOf(bySeverity, "Warning"), CountOf(bySeverity, "Information"),
            bySeverity, byStatus, byType, endpoints, timeline, bucketName);
    }

    public async Task<FiltersDto> GetFilters(string? project, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        string cprj = LogQueries.RequireProject(project);
        var (fromUtc, toUtc) = LogQueries.ResolveRange(from, to);
        var scope = _db.InScope(cprj, fromUtc, toUtc);

        var applications = await scope.Where(l => l.ApplicationName != null).Select(l => l.ApplicationName!).Distinct().OrderBy(x => x).ToListAsync(ct);
        var types = await scope.Where(l => l.ExceptionType != null).Select(l => l.ExceptionType!).Distinct().OrderBy(x => x).ToListAsync(ct);
        var codes = await scope.Where(l => l.StatusCode != null).Select(l => l.StatusCode!.Value).Distinct().OrderBy(x => x).ToListAsync(ct);
        return new FiltersDto(types, codes, applications);
    }

    private static int CountOf(List<SeverityCount> counts, string severity)
    {
        SeverityCount? found = counts.FirstOrDefault(x => x.Severity == severity);
        if (found == null)
        {
            return 0;
        }
        return found.Count;
    }

    private static string Truncate(string? value, int max)
    {
        if (value == null)
        {
            return "";
        }
        if (value.Length <= max)
        {
            return value;
        }
        return value.Substring(0, max);
    }

    /// <summary>Complète les seaux vides pour obtenir une courbe continue.</summary>
    private static List<TimelinePoint> BuildTimeline(
        IEnumerable<(int Bucket, string Level, int Count)> raw, DateTime fromUtc, DateTime toUtc, bool hourly)
    {
        // Pour chaque seau : [erreurs, avertissements, informations].
        var counts = new Dictionary<int, int[]>();
        foreach (var item in raw)
        {
            if (!counts.ContainsKey(item.Bucket))
            {
                counts[item.Bucket] = new int[3];
            }

            int column = 2;
            if (item.Level == "Error")
            {
                column = 0;
            }
            else if (item.Level == "Warning")
            {
                column = 1;
            }
            counts[item.Bucket][column] += item.Count;
        }

        int first;
        int last;
        if (hourly)
        {
            first = (int)(fromUtc - Epoch).TotalHours;
            last = (int)(toUtc - Epoch).TotalHours;
        }
        else
        {
            first = (int)(fromUtc - Epoch).TotalDays;
            last = (int)(toUtc - Epoch).TotalDays;
        }

        var points = new List<TimelinePoint>();
        for (int bucket = first; bucket <= last; bucket++)
        {
            DateTime start = Epoch.AddDays(bucket);
            if (hourly)
            {
                start = Epoch.AddHours(bucket);
            }

            int[] values = new int[3];
            if (counts.ContainsKey(bucket))
            {
                values = counts[bucket];
            }

            points.Add(new TimelinePoint(start, values[0], values[1], values[2]));
        }
        return points;
    }
}
