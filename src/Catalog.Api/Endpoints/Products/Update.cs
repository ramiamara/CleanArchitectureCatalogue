namespace Catalog.Api.Endpoints.Products;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

internal static class UpdateProduct
{
    internal static async Task<IResult> HandleAsync(
        Guid id,
        UpdateProductRequest request,
        IProductService svc,
        [FromServices] IValidator<UpdateProductRequest> validator,
        HttpContext ctx)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var userId = ctx.User.Identity?.Name ?? "anonymous";
        var dto = await svc.UpdateAsync(id, request, userId);
        return Results.Ok(new ApiResponse<ProductDto>(dto, traceId: ctx.TraceIdentifier));
    }
}