namespace Catalog.Api.Endpoints;

using Carter;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;

public class CatalogueEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalogues").WithTags("Catalogues");

        group.MapGet("/", GetAll)
             .WithName("GetAllCatalogues")
             .WithSummary("Get paginated catalogues")
             .AllowAnonymous()
             .Produces<ApiResponse<PagedResult<CatalogueDto>>>(200);

        group.MapGet("/{id:guid}", GetById)
             .WithName("GetCatalogueById")
             .AllowAnonymous()
             .Produces<ApiResponse<CatalogueDto>>(200)
             .Produces(404);

        group.MapPost("/", Create)
             .WithName("CreateCatalogue")
             .RequireAuthorization()
             .Produces<ApiResponse<CatalogueDto>>(201)
             .Produces(401);

        group.MapPut("/{id:guid}", Update)
             .WithName("UpdateCatalogue")
             .RequireAuthorization()
             .Produces<ApiResponse<CatalogueDto>>(200)
             .Produces(401).Produces(404);

        group.MapDelete("/{id:guid}", Delete)
             .WithName("DeleteCatalogue")
             .RequireAuthorization()
             .Produces(204).Produces(401).Produces(404);
    }

    private static async Task<IResult> GetAll(
        ICatalogueService svc, HttpContext ctx,
        int page = 1, int pageSize = 20)
    {
        var result = await svc.GetAllAsync(page, pageSize);
        ctx.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(new ApiResponse<PagedResult<CatalogueDto>>(result, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> GetById(Guid id, ICatalogueService svc, HttpContext ctx)
    {
        var catalogue = await svc.GetByIdAsync(id);
        if (catalogue is null)
            return Results.NotFound(new ApiResponse<object>(
                new ApiError { Code = "NOT_FOUND", Title = "Introuvable", Description = $"Catalogue {id} non trouve." },
                traceId: ctx.TraceIdentifier));
        ctx.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(new ApiResponse<CatalogueDto>(catalogue, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> Create(CreateCatalogueRequest request, ICatalogueService svc, HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name ?? "anonymous";
        var cat = await svc.CreateAsync(request, userId);
        return Results.Created($"/api/catalogues/{cat.Id}",
            new ApiResponse<CatalogueDto>(cat, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> Update(Guid id, CreateCatalogueRequest request, ICatalogueService svc, HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name ?? "anonymous";
        var cat = await svc.UpdateAsync(id, request, userId);
        return Results.Ok(new ApiResponse<CatalogueDto>(cat, traceId: ctx.TraceIdentifier));
    }

    private static async Task<IResult> Delete(Guid id, ICatalogueService svc, HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name ?? "anonymous";
        await svc.DeleteAsync(id, userId);
        return Results.NoContent();
    }
}
