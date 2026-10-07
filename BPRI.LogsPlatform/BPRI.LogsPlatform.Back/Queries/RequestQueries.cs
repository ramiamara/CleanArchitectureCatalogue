using BPRI.LogsPlatform.Back.Contracts;
using BPRI.LogsPlatform.Back.Data;
using Microsoft.EntityFrameworkCore;

namespace BPRI.LogsPlatform.Back.Queries;

internal static class RequestQueries
{
    /// <summary>Numéro d'erreur SQL Server : table inexistante.</summary>
    public const int MissingTableError = 208;

    public static IQueryable<RequestLogEntry> InRequestScope(this LogsDbContext db, string cprj, DateTime fromUtc, DateTime toUtc, string? application = null)
    {
        var query = db.Requests
            .AsNoTracking()
            .Where(r => r.Cprj == cprj && r.TimeStamp >= fromUtc && r.TimeStamp <= toUtc);

        if (!string.IsNullOrWhiteSpace(application))
        {
            string name = application.Trim();
            query = query.Where(r => r.ApplicationName == name);
        }
        return query;
    }

    public static IQueryable<RequestLogEntry> ApplyRequestFilter(this IQueryable<RequestLogEntry> query, RequestFilter filter)
    {
        if (filter.Method != null && filter.Method.Length > 0)
        {
            string[] methods = filter.Method.Select(m => m.Trim().ToUpperInvariant()).ToArray();
            query = query.Where(r => methods.Contains(r.HttpMethod!));
        }

        if (filter.StatusCode != null && filter.StatusCode.Length > 0)
        {
            int[] codes = filter.StatusCode;
            query = query.Where(r => codes.Contains(r.StatusCode!.Value));
        }

        if (filter.MinDurationMs.HasValue)
        {
            int min = filter.MinDurationMs.Value;
            query = query.Where(r => r.DurationMs >= min);
        }

        if (!string.IsNullOrWhiteSpace(filter.TraceId))
        {
            string trace = filter.TraceId.Trim();
            query = query.Where(r => r.TraceId!.StartsWith(trace));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(r => r.Path!.Contains(search)
                || r.UserName!.Contains(search)
                || r.RequestBody!.Contains(search)
                || r.ResponseBody!.Contains(search));
        }

        return query;
    }

    public static IQueryable<RequestLogEntry> SortRequests(this IQueryable<RequestLogEntry> query, string? field, string? direction)
    {
        bool ascending = string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
        string sortField = (field ?? "").ToLowerInvariant();

        if (sortField == "statuscode")
        {
            if (ascending)
            {
                return query.OrderBy(r => r.StatusCode).ThenBy(r => r.Id);
            }
            return query.OrderByDescending(r => r.StatusCode).ThenBy(r => r.Id);
        }

        if (sortField == "durationms")
        {
            if (ascending)
            {
                return query.OrderBy(r => r.DurationMs).ThenBy(r => r.Id);
            }
            return query.OrderByDescending(r => r.DurationMs).ThenBy(r => r.Id);
        }

        if (sortField == "method")
        {
            if (ascending)
            {
                return query.OrderBy(r => r.HttpMethod).ThenBy(r => r.Id);
            }
            return query.OrderByDescending(r => r.HttpMethod).ThenBy(r => r.Id);
        }

        if (sortField == "path")
        {
            if (ascending)
            {
                return query.OrderBy(r => r.Path).ThenBy(r => r.Id);
            }
            return query.OrderByDescending(r => r.Path).ThenBy(r => r.Id);
        }

        if (ascending)
        {
            return query.OrderBy(r => r.TimeStamp).ThenBy(r => r.Id);
        }
        return query.OrderByDescending(r => r.TimeStamp).ThenBy(r => r.Id);
    }
}
