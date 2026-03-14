namespace Catalog.Api.Endpoints.Products;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;
using Microsoft.AspNetCore.Mvc;

internal static class GetAllProducts
{
    internal static async Task<IResult> HandleAsync(
        IProductService svc,
        HttpContext ctx,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await svc.GetAllAsync(page, pageSize);
        return Results.Ok(new ApiResponse<PagedResult<ProductDto>>(result, traceId: ctx.TraceIdentifier));
    }
}