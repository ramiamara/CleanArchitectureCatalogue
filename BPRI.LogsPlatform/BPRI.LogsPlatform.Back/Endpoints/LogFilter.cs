namespace BPRI.LogsPlatform.Back.Endpoints;

/// <summary>Paramètres de requête de GET /api/logs (liés via [AsParameters]).</summary>
public sealed record LogFilter(
    string? Project,
    string[]? Severity,
    int[]? StatusCode,
    string? ExceptionType,
    string? TraceId,
    string? Search,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page = 1,
    int PageSize = 25,
    string? SortField = null,
    string? SortDir = null);
