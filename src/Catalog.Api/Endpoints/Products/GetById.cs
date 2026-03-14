namespace Catalog.Api.Endpoints.Products;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;

internal static class GetProductById
{
    internal static async Task<IResult> HandleAsync(Guid id, IProductService svc, HttpContext ctx)
    {
        var dto = await svc.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Produit {id} introuvable.");
        return Results.Ok(new ApiResponse<ProductDto>(dto, traceId: ctx.TraceIdentifier));
    }
}