namespace Catalog.Api.Endpoints.Products;
using Catalog.Application.Services;

internal static class DeleteProduct
{
    internal static async Task<IResult> HandleAsync(Guid id, IProductService svc, HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name ?? "anonymous";
        await svc.DeleteAsync(id, userId);
        return Results.NoContent();
    }
}