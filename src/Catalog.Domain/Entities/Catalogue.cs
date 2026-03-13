namespace Catalog.Domain.Entities;

using Catalog.Domain.Common;

public class Catalogue : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    private List<Product> _products = [];
    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    public Catalogue() : base() { }

    public Catalogue(string name, string? description = null) : base()
    {
        Name = name;
        Description = description;
        IsActive = true;
    }

    public void AddProduct(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (_products.Any(p => p.Id == product.Id))
            throw new InvalidOperationException("Ce produit existe déjà dans le catalogue.");

        _products.Add(product);
    }

    public void RemoveProduct(Guid productId)
    {
        var product = _products.FirstOrDefault(p => p.Id == productId);
        if (product != null)
            _products.Remove(product);
    }
}