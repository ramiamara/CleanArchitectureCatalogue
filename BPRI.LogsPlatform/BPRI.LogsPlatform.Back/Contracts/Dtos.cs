namespace BPRI.LogsPlatform.Back.Contracts;

// Severity : "Error" | "Warning" | "Information"
public sealed record ProjectSummaryDto(string Code, int TotalLogs, DateTime? LastOccurredAtUtc);

public sealed record LogListItemDto(
    int Id, string TraceId, string Cprj, string ApplicationName, string Severity, int? StatusCode,
    string? ExceptionType, string? SourceContext, string Message, string? HttpMethod, string? Path,
    string? UserName, DateTime OccurredAtUtc);

public sealed record LogDetailDto(
    int Id, string TraceId, string Cprj, string ApplicationName, string Severity, int? StatusCode,
    string? ExceptionType, string? SourceContext, string Message, string? HttpMethod, string? Path,
    string? UserName, DateTime OccurredAtUtc,
    string? UserId, string? Claims, string? Exception, string? InnerException,
    string? EnvironmentName, string? MachineName, string? Fingerprint);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record SeverityCount(string Severity, int Count);
public sealed record StatusCodeCount(int StatusCode, int Count);
public sealed record ExceptionTypeCount(string ExceptionType, int Count);
public sealed record EndpointCount(string? Method, string? Path, int Count);
public sealed record TimelinePoint(DateTime TimestampUtc, int Error, int Warning, int Information);

public sealed record StatsDto(
    int Total, int Errors, int Warnings, int Informations,
    IReadOnlyList<SeverityCount> BySeverity,
    IReadOnlyList<StatusCodeCount> ByStatusCode,
    IReadOnlyList<ExceptionTypeCount> ByExceptionType,
    IReadOnlyList<EndpointCount> TopEndpoints,
    IReadOnlyList<TimelinePoint> Timeline,
    string TimelineBucket);

public sealed record FiltersDto(IReadOnlyList<string> ExceptionTypes, IReadOnlyList<int> StatusCodes, IReadOnlyList<string> Applications);

// ---- Trace des requêtes (table RequestLogs)
public sealed record RequestListItemDto(
    int Id, string TraceId, string Cprj, string ApplicationName, string? HttpMethod, string? Path, string? QueryString,
    int? StatusCode, int? DurationMs, string? UserName, DateTime OccurredAtUtc);

public sealed record RequestDetailDto(
    int Id, string TraceId, string Cprj, string ApplicationName, string? HttpMethod, string? Path, string? QueryString,
    int? StatusCode, int? DurationMs, string? UserName, DateTime OccurredAtUtc,
    string? UserId, string? RequestBody, string? ResponseBody, string? EnvironmentName, string? MachineName);

public sealed record StatusClassCount(string StatusClass, int Count);
public sealed record MethodCount(string Method, int Count);
public sealed record SlowEndpoint(string? Method, string? Path, int Count, int AvgDurationMs, int MaxDurationMs);
public sealed record RequestTimelinePoint(DateTime TimestampUtc, int Success, int ClientError, int ServerError, int AvgDurationMs);

public sealed record RequestStatsDto(
    int Total, int ServerErrors, int ClientErrors, int AvgDurationMs, int P95DurationMs,
    IReadOnlyList<StatusClassCount> ByStatusClass,
    IReadOnlyList<StatusCodeCount> ByStatusCode,
    IReadOnlyList<MethodCount> ByMethod,
    IReadOnlyList<EndpointCount> TopEndpoints,
    IReadOnlyList<SlowEndpoint> SlowEndpoints,
    IReadOnlyList<RequestTimelinePoint> Timeline,
    string TimelineBucket);

public sealed record RequestFiltersDto(IReadOnlyList<string> Applications, IReadOnlyList<string> Methods, IReadOnlyList<int> StatusCodes);
