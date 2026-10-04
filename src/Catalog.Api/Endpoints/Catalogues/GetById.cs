namespace Catalog.Api.Endpoints.Catalogues;
using Catalog.Application.DTOs;
using Catalog.Application.Services;

internal static class GetCatalogueById
{
    internal static async Task<IResult> HandleAsync(Guid id, ICatalogueService svc)
    {
        var dto = await svc.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Catalogue {id} introuvable.");
        return Results.Ok(dto);
    }
}