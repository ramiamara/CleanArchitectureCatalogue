using BPRI.LogsPlatform.Back.Contracts;
using BPRI.LogsPlatform.Back.Services;
using Microsoft.AspNetCore.Mvc;

namespace BPRI.LogsPlatform.Back.Controllers;

[ApiController]
[Route("api")]
public sealed class LogsController : ControllerBase
{
    private readonly ILogService _service;

    public LogsController(ILogService service)
    {
        _service = service;
    }

    [HttpGet("logs")]
    public Task<PagedResult<LogListItemDto>> GetLogs([FromQuery] LogFilter filter, CancellationToken ct)
    {
        return _service.GetLogs(filter, ct);
    }

    [HttpGet("logs/{id:int}")]
    public Task<LogDetailDto> GetLog(int id, CancellationToken ct)
    {
        return _service.GetLog(id, ct);
    }

    [HttpGet("stats")]
    public Task<StatsDto> GetStats(string? project, string? application, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        return _service.GetStats(project, application, from, to, ct);
    }

    [HttpGet("filters")]
    public Task<FiltersDto> GetFilters(string? project, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        return _service.GetFilters(project, from, to, ct);
    }
}
