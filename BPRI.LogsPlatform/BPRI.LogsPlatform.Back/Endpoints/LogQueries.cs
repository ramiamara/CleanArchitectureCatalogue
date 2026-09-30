using BPRI.ExceptionHandling;
using BPRI.LogsPlatform.Back.Data;
using Microsoft.EntityFrameworkCore;

namespace BPRI.LogsPlatform.Back.Endpoints;

internal static class LogQueries
{
    public const int MaxRangeDays = 366;
    public const int MaxPageSize = 200;

    public static IQueryable<LogEntry> InScope(this LogsDbContext db, string cprj, DateTime fromUtc, DateTime toUtc)
    {
        return db.Logs
            .AsNoTracking()
            .Where(l => l.Cprj == cprj && l.TimeStamp >= fromUtc && l.TimeStamp <= toUtc);
    }

    public static IQueryable<LogEntry> ApplyFilter(this IQueryable<LogEntry> query, LogFilter filter)
    {
        if (filter.Severity != null && filter.Severity.Length > 0)
        {
            var levels = new List<string>();
            foreach (string severity in filter.Severity)
            {
                levels.AddRange(ExpandLevel(severity));
            }
            if (levels.Count > 0)
            {
                query = query.Where(l => levels.Contains(l.Level!));
            }
        }

        if (filter.StatusCode != null && filter.StatusCode.Length > 0)
        {
            int[] codes = filter.StatusCode;
            query = query.Where(l => codes.Contains(l.StatusCode!.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.ExceptionType))
        {
            query = query.Where(l => l.ExceptionType == filter.ExceptionType);
        }

        if (!string.IsNullOrWhiteSpace(filter.TraceId))
        {
            string trace = filter.TraceId.Trim();
            query = query.Where(l => l.TraceId!.StartsWith(trace));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(l => l.Message!.Contains(search)
                || l.Path!.Contains(search)
                || l.ExceptionType!.Contains(search)
                || l.SourceContext!.Contains(search)
                || l.UserName!.Contains(search));
        }

        return query;
    }

    public static IQueryable<LogEntry> Sort(this IQueryable<LogEntry> query, string? field, string? direction)
    {
        bool ascending = string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
        string sortField = (field ?? "").ToLowerInvariant();

        if (sortField == "severity")
        {
            if (ascending)
            {
                return query.OrderBy(l => l.Level == "Error" || l.Level == "Fatal" ? 2 : l.Level == "Warning" ? 1 : 0).ThenBy(l => l.Id);
            }
            return query.OrderByDescending(l => l.Level == "Error" || l.Level == "Fatal" ? 2 : l.Level == "Warning" ? 1 : 0).ThenBy(l => l.Id);
        }

        if (sortField == "statuscode")
        {
            if (ascending)
            {
                return query.OrderBy(l => l.StatusCode).ThenBy(l => l.Id);
            }
            return query.OrderByDescending(l => l.StatusCode).ThenBy(l => l.Id);
        }

        if (sortField == "exceptiontype")
        {
            if (ascending)
            {
                return query.OrderBy(l => l.ExceptionType).ThenBy(l => l.Id);
            }
            return query.OrderByDescending(l => l.ExceptionType).ThenBy(l => l.Id);
        }

        if (ascending)
        {
            return query.OrderBy(l => l.TimeStamp).ThenBy(l => l.Id);
        }
        return query.OrderByDescending(l => l.TimeStamp).ThenBy(l => l.Id);
    }

    /// <summary>Période par défaut : 24 h. Maximum : 366 jours.</summary>
    public static (DateTime FromUtc, DateTime ToUtc) ResolveRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        DateTime toUtc = DateTime.UtcNow;
        if (to.HasValue)
        {
            toUtc = to.Value.UtcDateTime;
        }

        DateTime fromUtc = toUtc.AddHours(-24);
        if (from.HasValue)
        {
            fromUtc = from.Value.UtcDateTime;
        }

        if (fromUtc > toUtc)
        {
            throw new AppException("La date de début doit précéder la date de fin.", 400);
        }
        if ((toUtc - fromUtc).TotalDays > MaxRangeDays)
        {
            throw new AppException("La période ne peut pas dépasser " + MaxRangeDays + " jours.", 400);
        }

        return (fromUtc, toUtc);
    }

    /// <summary>Les dates lues en base n'ont pas de Kind : on les déclare UTC.</summary>
    public static DateTime AsUtc(this DateTime date)
    {
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    /// <summary>Fatal devient Error ; Debug et Verbose deviennent Information.</summary>
    public static string NormalizeLevel(string? level)
    {
        string value = (level ?? "").Trim().ToLowerInvariant();
        return value == "error" || value == "fatal" ? "Error" : value == "warning" ? "Warning" : "Information";
    }

    private static string[] ExpandLevel(string? severity)
    {
        string value = (severity ?? "").Trim().ToLowerInvariant();
        if (value == "error")
        {
            return new[] { "Error", "Fatal" };
        }
        if (value == "warning")
        {
            return new[] { "Warning" };
        }
        if (value == "info" || value == "information")
        {
            return new[] { "Information", "Debug", "Verbose" };
        }
        return new string[0];
    }
}
