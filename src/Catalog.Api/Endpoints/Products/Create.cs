namespace Catalog.Api.Endpoints.Products;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

internal static class CreateProduct
{
    internal static async Task<IResult> HandleAsync(
        CreateProductRequest request,
        IProductService svc,
        [FromServices] IValidator<CreateProductRequest> validator,
        HttpContext ctx)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var userId = ctx.User.Identity?.Name ?? "anonymous";
        var dto = await svc.CreateAsync(request, userId);
        return Results.Created($"/api/products/{dto.Id}",
            new ApiResponse<ProductDto>(dto, traceId: ctx.TraceIdentifier));
    }
}