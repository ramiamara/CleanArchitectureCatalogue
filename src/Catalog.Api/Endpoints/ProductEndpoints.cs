namespace Catalog.Api.Endpoints;

using Carter;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;

public class ProductEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetAll)
             .WithName("GetAllProducts")
             .AllowAnonymous()
             .Produces<ApiResponse<PagedResult<ProductDto>>>(200);

        group.MapGet("/{id:guid}", GetById)
             .WithName("GetProductById")
             .AllowAnonymous()
             .Produces<ApiResponse<ProductDto>>(200).Produces(404);

        group.MapGet("/catalogue/{catalogueId:guid}", GetByCatalogue)
             .WithName("GetProductsByCatalogue")
             .AllowAnonymous()
             .Produces<ApiResponse<IEnumerable<ProductDto>>>(200);

        group.MapPost("/", Create)
             .WithName("CreateProduct")
             .RequireAuthorization()
             .Produces<ApiResponse<ProductDto>>(201).Produces(401);

        group.MapPut("/{id:guid}", Update)
             .WithName("UpdateProduct")
             .RequireAuthorization()
             .Produces<ApiResponse<ProductDto>>(200).Produces(401).Produces(404);

        group.MapDelete("/{id:guid}", Delete)
             .WithName("DeleteProduct")
             .RequireAuthorization()
             .Produces(204).Produces(401).Produces(404);
    }

    private static async Task<IResult> GetAll(IProductService svc, HttpContext ctx, int page = 1, int pageSize = 20)
    {
        var result = await svc.GetAllAsync(page, pageSize);
        ctx.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(new ApiResponse<PagedResult<ProductDto>>(result, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> GetById(Guid id, IProductService svc, HttpContext ctx)
    {
        var product = await svc.GetByIdAsync(id);
        if (product is null)
            return Results.NotFound(new ApiResponse<object>(
                new ApiError { Code = "NOT_FOUND", Title = "Introuvable", Description = $"Produit {id} non trouve." },
                traceId: ctx.TraceIdentifier));
        ctx.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(new ApiResponse<ProductDto>(product, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> GetByCatalogue(Guid catalogueId, IProductService svc, HttpContext ctx)
    {
        var products = await svc.GetByCatalogueAsync(catalogueId);
        ctx.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(new ApiResponse<IEnumerable<ProductDto>>(products, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> Create(CreateProductRequest request, IProductService svc, HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name ?? "anonymous";
        var product = await svc.CreateAsync(request, userId);
        return Results.Created($"/api/products/{product.Id}",
            new ApiResponse<ProductDto>(product, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> Update(Guid id, CreateProductRequest request, IProductService svc, HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name ?? "anonymous";
        var product = await svc.UpdateAsync(id, request, userId);
        return Results.Ok(new ApiResponse<ProductDto>(product, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> Delete(Guid id, IProductService svc, HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name ?? "anonymous";
        await svc.DeleteAsync(id, userId);
        return Results.NoContent();
    }
}
