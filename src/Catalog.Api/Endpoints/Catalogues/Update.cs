namespace Catalog.Api.Endpoints.Catalogues;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

internal static class UpdateCatalogue
{
    internal static async Task<IResult> HandleAsync(
        Guid id,
        UpdateCatalogueRequest request,
        ICatalogueService svc,
        [FromServices] IValidator<UpdateCatalogueRequest> validator,
        HttpContext ctx)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var userId = ctx.User.Identity?.Name ?? "anonymous";
        var dto = await svc.UpdateAsync(id, request, userId);
        return Results.Ok(new ApiResponse<CatalogueDto>(dto, traceId: ctx.TraceIdentifier));
    }
}