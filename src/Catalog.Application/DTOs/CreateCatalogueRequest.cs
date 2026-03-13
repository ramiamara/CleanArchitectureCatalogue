namespace Catalog.Application.DTOs;

public record CreateCatalogueRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}