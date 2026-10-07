namespace BPRI.LogsPlatform.Back.Contracts;

/// <summary>Paramètres de GET /api/logs.</summary>
public sealed class LogFilter
{
    public string? Project { get; set; }
    public string? Application { get; set; }
    public string[]? Severity { get; set; }
    public int[]? StatusCode { get; set; }
    public string? ExceptionType { get; set; }
    public string? TraceId { get; set; }
    public string? Search { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortField { get; set; }
    public string? SortDir { get; set; }
}
