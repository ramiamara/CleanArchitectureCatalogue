using BPRI.LogsPlatform.Back.Contracts;
using BPRI.LogsPlatform.Back.Services;
using Microsoft.AspNetCore.Mvc;

namespace BPRI.LogsPlatform.Back.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectsController(IProjectService service)
    {
        _service = service;
    }

    /// <summary>Liste des cprj présents en base.</summary>
    [HttpGet]
    public Task<List<ProjectSummaryDto>> GetProjects(CancellationToken ct)
    {
        return _service.GetProjects(ct);
    }
}
