namespace Catalog.Api.Endpoints.Products;
using Catalog.Application.DTOs;
using Catalog.Application.Services;
using Microsoft.AspNetCore.Mvc;

internal static class GetAllProducts
{
    internal static async Task<IResult> HandleAsync(
        IProductService svc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await svc.GetAllAsync(page, pageSize);
        return Results.Ok(result);
    }
}