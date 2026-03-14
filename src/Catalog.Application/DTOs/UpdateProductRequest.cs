namespace Catalog.Application.DTOs;

public record UpdateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "EUR";
    public int Quantity { get; init; }
}