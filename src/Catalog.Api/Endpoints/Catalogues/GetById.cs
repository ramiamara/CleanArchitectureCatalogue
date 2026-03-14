namespace Catalog.Api.Endpoints.Catalogues;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;

internal static class GetCatalogueById
{
    internal static async Task<IResult> HandleAsync(Guid id, ICatalogueService svc, HttpContext ctx)
    {
        var dto = await svc.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Catalogue {id} introuvable.");
        return Results.Ok(new ApiResponse<CatalogueDto>(dto, traceId: ctx.TraceIdentifier));
    }
}