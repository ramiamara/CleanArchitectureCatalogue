namespace Catalog.Api.Endpoints.Products;
using Carter;

/// <summary>
/// Carter module — registers all /api/products routes.
/// Business logic lives in the feature handler files (GetAll, GetById, Create, Update, Delete).
/// </summary>
public class ProductEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
                       .RequireAuthorization()
                       .WithTags("Products");

        group.MapGet("/", GetAllProducts.HandleAsync).WithName("GetAllProducts").Produces(200).Produces(401);
        group.MapGet("/{id:guid}", GetProductById.HandleAsync).WithName("GetProductById").Produces(200).Produces(401).Produces(404);
        group.MapPost("/", CreateProduct.HandleAsync).WithName("CreateProduct").Produces(201).Produces(400).Produces(401);
        group.MapPut("/{id:guid}", UpdateProduct.HandleAsync).WithName("UpdateProduct").Produces(200).Produces(400).Produces(401).Produces(404);
        group.MapDelete("/{id:guid}", DeleteProduct.HandleAsync).WithName("DeleteProduct").Produces(204).Produces(401).Produces(404);
    }
}