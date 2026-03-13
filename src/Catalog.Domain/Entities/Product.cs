namespace Catalog.Domain.Entities;

using Catalog.Domain.Common;
using Catalog.Domain.ValueObjects;

public class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Money Price { get; set; } = Money.Zero();
    public int Quantity { get; set; }
    public Guid CatalogueId { get; set; }

    public Product() : base() { }

    public Product(string name, Money price, int quantity = 0, string? description = null) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Le nom du produit est requis.", nameof(name));

        if (quantity < 0)
            throw new ArgumentException("La quantité ne peut pas être négative.", nameof(quantity));

        Name = name;
        Price = price;
        Quantity = quantity;
        Description = description;
    }

    public void UpdateStock(int quantity, string modifiedBy)
    {
        if (quantity < 0)
            throw new ArgumentException("La quantité ne peut pas être négative.", nameof(quantity));

        Quantity = quantity;
        MarkAsModified(modifiedBy);
    }

    public void DecreaseStock(int quantity, string modifiedBy)
    {
        if (Quantity < quantity)
            throw new InvalidOperationException("Stock insuffisant.");

        Quantity -= quantity;
        MarkAsModified(modifiedBy);
    }
}