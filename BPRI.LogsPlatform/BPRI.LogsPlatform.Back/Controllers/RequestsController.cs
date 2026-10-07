using BPRI.LogsPlatform.Back.Contracts;
using BPRI.LogsPlatform.Back.Services;
using Microsoft.AspNetCore.Mvc;

namespace BPRI.LogsPlatform.Back.Controllers;

[ApiController]
[Route("api/requests")]
public sealed class RequestsController : ControllerBase
{
    private readonly IRequestService _service;

    public RequestsController(IRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<PagedResult<RequestListItemDto>> GetRequests([FromQuery] RequestFilter filter, CancellationToken ct)
    {
        return _service.GetRequests(filter, ct);
    }

    [HttpGet("{id:int}")]
    public Task<RequestDetailDto> GetRequest(int id, CancellationToken ct)
    {
        return _service.GetRequest(id, ct);
    }

    [HttpGet("stats")]
    public Task<RequestStatsDto> GetStats(string? project, string? application, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        return _service.GetStats(project, application, from, to, ct);
    }

    [HttpGet("filters")]
    public Task<RequestFiltersDto> GetFilters(string? project, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        return _service.GetFilters(project, from, to, ct);
    }
}
