using System.Text.RegularExpressions;
using BPRI.ExceptionHandling;
using BPRI.LogsPlatform.Back.Contracts;
using BPRI.LogsPlatform.Back.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BPRI.LogsPlatform.Back.Endpoints;

internal static class RequestsEndpoints
{
    private const int MissingTableError = 208;
    private const int MaxEndpointGroups = 500;

    private static readonly DateTime Epoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Regex IdSegment = new(@"/(\d+|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?=/|$)", RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapRequestsEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/requests");
        api.MapGet("", GetRequests);
        api.MapGet("/{id:int}", GetRequest);
        api.MapGet("/stats", GetStats);
        api.MapGet("/filters", GetFilters);
        return app;
    }

    private static async Task<IResult> GetRequests([AsParameters] RequestFilter f, LogsDbContext db, CancellationToken ct)
    {
        var cprj = LogsEndpoints.RequireProject(f.Project);
        var (from, to) = LogQueries.ResolveRange(f.From, f.To);
        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, LogQueries.MaxPageSize);

        try
        {
            var query = db.InRequestScope(cprj, from, to, f.Application).ApplyRequestFilter(f);
            var total = await query.CountAsync(ct);

            var rows = await query.SortRequests(f.SortField, f.SortDir)
                .Skip((page - 1) * size).Take(size)
                .Select(r => new { r.Id, r.TraceId, r.Cprj, r.ApplicationName, r.HttpMethod, r.Path, r.QueryString, r.StatusCode, r.DurationMs, r.UserName, r.TimeStamp })
                .ToListAsync(ct);

            var items = rows.Select(r => new RequestListItemDto(
                r.Id, r.TraceId ?? "", r.Cprj ?? "", r.ApplicationName ?? "", r.HttpMethod, r.Path, r.QueryString,
                r.StatusCode, r.DurationMs, r.UserName, r.TimeStamp.AsUtc())).ToList();

            return Results.Ok(new PagedResult<RequestListItemDto>(items, total, page, size));
        }
        catch (SqlException ex) when (ex.Number == MissingTableError)
        {
            return Results.Ok(new PagedResult<RequestListItemDto>(new List<RequestListItemDto>(), 0, page, size));
        }
    }

    private static async Task<IResult> GetRequest(int id, LogsDbContext db, CancellationToken ct)
    {
        RequestLogEntry? r = null;
        try
        {
            r = await db.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        }
        catch (SqlException ex) when (ex.Number == MissingTableError)
        {
        }

        if (r == null)
        {
            throw new AppException("Requête " + id + " introuvable.", 404);
        }

        return Results.Ok(new RequestDetailDto(
            r.Id, r.TraceId ?? "", r.Cprj ?? "", r.ApplicationName ?? "", r.HttpMethod, r.Path, r.QueryString,
            r.StatusCode, r.DurationMs, r.UserName, r.TimeStamp.AsUtc(),
            r.UserId, r.RequestBody, r.ResponseBody, r.EnvironmentName, r.MachineName));
    }

    private static async Task<IResult> GetStats(
        [FromQuery] string? project, [FromQuery] string? application, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        LogsDbContext db, CancellationToken ct)
    {
        var cprj = LogsEndpoints.RequireProject(project);
        var (fromUtc, toUtc) = LogQueries.ResolveRange(from, to);
        var scope = db.InRequestScope(cprj, fromUtc, toUtc, application);
        var hourly = (toUtc - fromUtc).TotalDays <= 2;

        try
        {
            var statusRows = await scope.Where(r => r.StatusCode != null)
                .GroupBy(r => r.StatusCode!.Value)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var byStatus = statusRows.OrderBy(x => x.Code).Select(x => new StatusCodeCount(x.Code, x.Count)).ToList();

            var methodRows = await scope.Where(r => r.HttpMethod != null)
                .GroupBy(r => r.HttpMethod!)
                .Select(g => new { Method = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var byMethod = methodRows.OrderByDescending(x => x.Count).Select(x => new MethodCount(x.Method, x.Count)).ToList();

            var total = await scope.CountAsync(ct);
            var avg = await scope.AverageAsync(r => (double?)r.DurationMs, ct) ?? 0;
            var p95 = await ComputeP95(scope, ct);

            var groups = await scope.Where(r => r.Path != null)
                .GroupBy(r => new { r.HttpMethod, r.Path })
                .Select(g => new
                {
                    g.Key.HttpMethod,
                    g.Key.Path,
                    Count = g.Count(),
                    Avg = g.Average(x => (double?)x.DurationMs),
                    Max = g.Max(x => x.DurationMs)
                })
                .OrderByDescending(x => x.Count).Take(MaxEndpointGroups)
                .ToListAsync(ct);

            var endpoints = groups
                .GroupBy(x => (x.HttpMethod, Path: NormalizePath(x.Path!)))
                .Select(g => new
                {
                    g.Key.HttpMethod,
                    g.Key.Path,
                    Count = g.Sum(x => x.Count),
                    Avg = g.Sum(x => (x.Avg ?? 0) * x.Count) / g.Sum(x => x.Count),
                    Max = g.Max(x => x.Max ?? 0)
                })
                .ToList();

            var top = endpoints.OrderByDescending(x => x.Count).Take(8)
                .Select(x => new EndpointCount(x.HttpMethod, x.Path, x.Count)).ToList();
            var slow = endpoints.OrderByDescending(x => x.Avg).Take(8)
                .Select(x => new SlowEndpoint(x.HttpMethod, x.Path, x.Count, (int)Math.Round(x.Avg), x.Max)).ToList();

            var timeline = await BuildTimeline(scope, fromUtc, toUtc, hourly, ct);

            var byClass = new List<StatusClassCount>();
            foreach (string label in new[] { "2xx", "3xx", "4xx", "5xx" })
            {
                int start = (label[0] - '0') * 100;
                int count = byStatus.Where(x => x.StatusCode >= start && x.StatusCode < start + 100).Sum(x => x.Count);
                byClass.Add(new StatusClassCount(label, count));
            }

            string bucketName = "day";
            if (hourly)
            {
                bucketName = "hour";
            }

            return Results.Ok(new RequestStatsDto(
                total,
                byStatus.Where(x => x.StatusCode >= 500).Sum(x => x.Count),
                byStatus.Where(x => x.StatusCode >= 400 && x.StatusCode < 500).Sum(x => x.Count),
                (int)Math.Round(avg), p95, byClass, byStatus, byMethod, top, slow, timeline, bucketName));
        }
        catch (SqlException ex) when (ex.Number == MissingTableError)
        {
            return Results.Ok(new RequestStatsDto(
                0, 0, 0, 0, 0, new List<StatusClassCount>(), new List<StatusCodeCount>(), new List<MethodCount>(),
                new List<EndpointCount>(), new List<SlowEndpoint>(), new List<RequestTimelinePoint>(), "hour"));
        }
    }

    private static async Task<IResult> GetFilters(
        [FromQuery] string? project, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        LogsDbContext db, CancellationToken ct)
    {
        var cprj = LogsEndpoints.RequireProject(project);
        var (fromUtc, toUtc) = LogQueries.ResolveRange(from, to);
        var scope = db.InRequestScope(cprj, fromUtc, toUtc);

        try
        {
            var applications = await scope.Where(r => r.ApplicationName != null).Select(r => r.ApplicationName!).Distinct().OrderBy(x => x).ToListAsync(ct);
            var methods = await scope.Where(r => r.HttpMethod != null).Select(r => r.HttpMethod!).Distinct().OrderBy(x => x).ToListAsync(ct);
            var codes = await scope.Where(r => r.StatusCode != null).Select(r => r.StatusCode!.Value).Distinct().OrderBy(x => x).ToListAsync(ct);
            return Results.Ok(new RequestFiltersDto(applications, methods, codes));
        }
        catch (SqlException ex) when (ex.Number == MissingTableError)
        {
            return Results.Ok(new RequestFiltersDto(new List<string>(), new List<string>(), new List<int>()));
        }
    }

    private static async Task<int> ComputeP95(IQueryable<RequestLogEntry> scope, CancellationToken ct)
    {
        var timed = scope.Where(r => r.DurationMs != null);
        int count = await timed.CountAsync(ct);
        if (count == 0)
        {
            return 0;
        }

        int skip = Math.Min(count - 1, (int)(count * 0.95));
        int? value = await timed.OrderBy(r => r.DurationMs).Skip(skip).Select(r => r.DurationMs).FirstOrDefaultAsync(ct);
        return value ?? 0;
    }

    private static string NormalizePath(string path)
    {
        return IdSegment.Replace(path, "/{id}");
    }

    private static async Task<List<RequestTimelinePoint>> BuildTimeline(
        IQueryable<RequestLogEntry> scope, DateTime fromUtc, DateTime toUtc, bool hourly, CancellationToken ct)
    {
        var raw = new List<(int Bucket, int Success, int Client, int Server, int Avg)>();
        if (hourly)
        {
            var rows = await scope
                .GroupBy(r => EF.Functions.DateDiffHour(Epoch, r.TimeStamp))
                .Select(g => new
                {
                    Bucket = g.Key,
                    Success = g.Count(x => x.StatusCode == null || x.StatusCode < 400),
                    Client = g.Count(x => x.StatusCode >= 400 && x.StatusCode < 500),
                    Server = g.Count(x => x.StatusCode >= 500),
                    Avg = g.Average(x => (double?)x.DurationMs)
                })
                .ToListAsync(ct);
            foreach (var row in rows)
            {
                raw.Add((row.Bucket, row.Success, row.Client, row.Server, (int)Math.Round(row.Avg ?? 0)));
            }
        }
        else
        {
            var rows = await scope
                .GroupBy(r => EF.Functions.DateDiffDay(Epoch, r.TimeStamp))
                .Select(g => new
                {
                    Bucket = g.Key,
                    Success = g.Count(x => x.StatusCode == null || x.StatusCode < 400),
                    Client = g.Count(x => x.StatusCode >= 400 && x.StatusCode < 500),
                    Server = g.Count(x => x.StatusCode >= 500),
                    Avg = g.Average(x => (double?)x.DurationMs)
                })
                .ToListAsync(ct);
            foreach (var row in rows)
            {
                raw.Add((row.Bucket, row.Success, row.Client, row.Server, (int)Math.Round(row.Avg ?? 0)));
            }
        }

        var byBucket = raw.ToDictionary(x => x.Bucket);

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

        var points = new List<RequestTimelinePoint>();
        for (int bucket = first; bucket <= last; bucket++)
        {
            DateTime start = Epoch.AddDays(bucket);
            if (hourly)
            {
                start = Epoch.AddHours(bucket);
            }

            if (byBucket.TryGetValue(bucket, out var item))
            {
                points.Add(new RequestTimelinePoint(start, item.Success, item.Client, item.Server, item.Avg));
            }
            else
            {
                points.Add(new RequestTimelinePoint(start, 0, 0, 0, 0));
            }
        }
        return points;
    }
}
