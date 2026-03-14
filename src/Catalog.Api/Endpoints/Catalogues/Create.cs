namespace Catalog.Api.Endpoints.Catalogues;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Catalog.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

internal static class CreateCatalogue
{
    internal static async Task<IResult> HandleAsync(
        CreateCatalogueRequest request,
        ICatalogueService svc,
        [FromServices] IValidator<CreateCatalogueRequest> validator,
        HttpContext ctx)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var userId = ctx.User.Identity?.Name ?? "anonymous";
        var dto = await svc.CreateAsync(request, userId);
        return Results.Created($"/api/catalogues/{dto.Id}",
            new ApiResponse<CatalogueDto>(dto, traceId: ctx.TraceIdentifier));
    }
}