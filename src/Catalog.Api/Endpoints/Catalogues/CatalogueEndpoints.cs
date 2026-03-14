namespace Catalog.Api.Endpoints.Catalogues;
using Carter;

/// <summary>
/// Carter module — registers all /api/catalogues routes.
/// Business logic lives in the feature handler files (GetAll, GetById, Create, Update, Delete).
/// </summary>
public class CatalogueEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalogues")
                       .RequireAuthorization()
                       .WithTags("Catalogues");

        group.MapGet("/", GetAllCatalogues.HandleAsync).WithName("GetAllCatalogues").Produces(200);
        group.MapGet("/{id:guid}", GetCatalogueById.HandleAsync).WithName("GetCatalogueById").Produces(200).Produces(401).Produces(404);
        group.MapPost("/", CreateCatalogue.HandleAsync).WithName("CreateCatalogue").Produces(201).Produces(400).Produces(401);
        group.MapPut("/{id:guid}", UpdateCatalogue.HandleAsync).WithName("UpdateCatalogue").Produces(200).Produces(400).Produces(401).Produces(404);
        group.MapDelete("/{id:guid}", DeleteCatalogue.HandleAsync).WithName("DeleteCatalogue").Produces(204).Produces(401).Produces(404);
    }
}