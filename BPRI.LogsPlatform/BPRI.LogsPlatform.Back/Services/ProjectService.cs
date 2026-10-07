using BPRI.LogsPlatform.Back.Contracts;
using BPRI.LogsPlatform.Back.Data;
using BPRI.LogsPlatform.Back.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BPRI.LogsPlatform.Back.Services;

public interface IProjectService
{
    Task<List<ProjectSummaryDto>> GetProjects(CancellationToken ct);
}

/// <summary>Liste des cprj lus dans dbo.ExceptionLogs (colonne Cprj), complétés par ceux de dbo.RequestLogs.</summary>
public sealed class ProjectService : IProjectService
{
    private readonly LogsDbContext _db;

    public ProjectService(LogsDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProjectSummaryDto>> GetProjects(CancellationToken ct)
    {
        var rows = await _db.Logs
            .Where(l => l.Cprj != null)
            .GroupBy(l => l.Cprj!)
            .Select(g => new { Code = g.Key, Total = g.Count(), Last = g.Max(x => x.TimeStamp) })
            .ToListAsync(ct);

        var projects = rows.Select(r => new ProjectSummaryDto(r.Code, r.Total, r.Last.AsUtc())).ToList();

        foreach (string code in await GetRequestProjects(ct))
        {
            if (!projects.Any(p => p.Code == code))
            {
                projects.Add(new ProjectSummaryDto(code, 0, null));
            }
        }

        return projects.OrderBy(p => p.Code).ToList();
    }

    private async Task<List<string>> GetRequestProjects(CancellationToken ct)
    {
        try
        {
            return await _db.Requests
                .Where(r => r.Cprj != null)
                .Select(r => r.Cprj!)
                .Distinct()
                .ToListAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == RequestQueries.MissingTableError)
        {
            return new List<string>();
        }
    }
}
