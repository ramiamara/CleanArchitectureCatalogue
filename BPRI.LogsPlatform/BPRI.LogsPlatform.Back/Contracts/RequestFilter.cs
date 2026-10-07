namespace BPRI.LogsPlatform.Back.Contracts;

/// <summary>Paramètres de GET /api/requests.</summary>
public sealed class RequestFilter
{
    public string? Project { get; set; }
    public string? Application { get; set; }
    public string[]? Method { get; set; }
    public int[]? StatusCode { get; set; }
    public string? TraceId { get; set; }
    public string? Search { get; set; }
    public int? MinDurationMs { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortField { get; set; }
    public string? SortDir { get; set; }
}
