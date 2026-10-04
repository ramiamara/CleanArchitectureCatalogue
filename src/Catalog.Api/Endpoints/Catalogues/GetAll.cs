namespace Catalog.Api.Endpoints.Catalogues;
using Catalog.Application.DTOs;
using Catalog.Application.Services;
using Microsoft.AspNetCore.Mvc;

internal static class GetAllCatalogues
{
    internal static async Task<IResult> HandleAsync(
        ICatalogueService svc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await svc.GetAllAsync(page, pageSize);
        return Results.Ok(result);
    }
}