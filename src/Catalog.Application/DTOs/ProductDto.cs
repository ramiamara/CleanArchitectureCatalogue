namespace Catalog.Application.DTOs;

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "EUR";
    public int Quantity { get; init; }
    public Guid CatalogueId { get; init; }
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? ModifiedOn { get; init; }
    public string? ModifiedBy { get; init; }
}