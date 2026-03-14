namespace Catalog.Application.DTOs;

public record UpdateCatalogueRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}