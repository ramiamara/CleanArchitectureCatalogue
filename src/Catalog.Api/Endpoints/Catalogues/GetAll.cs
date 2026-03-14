namespace Catalog.Api.Endpoints.Catalogues;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;
using Microsoft.AspNetCore.Mvc;

internal static class GetAllCatalogues
{
    internal static async Task<IResult> HandleAsync(
        ICatalogueService svc,
        HttpContext ctx,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await svc.GetAllAsync(page, pageSize);
        return Results.Ok(new ApiResponse<PagedResult<CatalogueDto>>(result, traceId: ctx.TraceIdentifier));
    }
}