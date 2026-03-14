namespace Catalog.Api.Endpoints.Catalogues;
using Catalog.Application.Services;

internal static class DeleteCatalogue
{
    internal static async Task<IResult> HandleAsync(Guid id, ICatalogueService svc, HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name ?? "anonymous";
        await svc.DeleteAsync(id, userId);
        return Results.NoContent();
    }
}