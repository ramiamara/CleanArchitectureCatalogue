namespace Catalog.Application.DTOs;

public record CatalogueDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? ModifiedOn { get; init; }
    public string? ModifiedBy { get; init; }
}