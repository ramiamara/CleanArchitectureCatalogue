namespace Catalog.Infrastructure.Data;

using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seeds example catalogues and products into the database if it is empty.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(CatalogueContext context)
    {
        // Only seed if there are no catalogues yet
        if (await context.Catalogues.AnyAsync())
            return;

        // --- Catalogues ---
        var electronique = new Catalogue
        {
            Id = Guid.NewGuid(),
            Name = "Électronique",
            Description = "Smartphones, tablettes, ordinateurs et accessoires high-tech.",
            IsActive = true,
            CreatedBy = "seeder",
            CreatedOn = DateTime.UtcNow
        };

        var vetements = new Catalogue
        {
            Id = Guid.NewGuid(),
            Name = "Vêtements & Mode",
            Description = "Collections homme, femme et enfant pour toutes les saisons.",
            IsActive = true,
            CreatedBy = "seeder",
            CreatedOn = DateTime.UtcNow
        };

        var sport = new Catalogue
        {
            Id = Guid.NewGuid(),
            Name = "Sport & Fitness",
            Description = "Équipements, chaussures et tenues de sport.",
            IsActive = true,
            CreatedBy = "seeder",
            CreatedOn = DateTime.UtcNow
        };

        await context.Catalogues.AddRangeAsync(electronique, vetements, sport);
        await context.SaveChangesAsync();

        // --- Products : Électronique ---
        var products = new List<Product>
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "iPhone 15 Pro",
                Description = "Smartphone Apple avec puce A17 Pro, 256 Go, Titane naturel.",
                Price = new Money(1299m, "EUR"),
                Quantity = 50,
                CatalogueId = electronique.Id,
                CreatedBy = "seeder",
                CreatedOn = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Samsung Galaxy S24 Ultra",
                Description = "Smartphone Android 12 Go RAM, S-Pen inclus, appareil photo 200 MP.",
                Price = new Money(1199m, "EUR"),
                Quantity = 35,
                CatalogueId = electronique.Id,
                CreatedBy = "seeder",
                CreatedOn = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "MacBook Pro 14\"",
                Description = "Ordinateur portable Apple M3 Pro, 18 Go RAM, 512 Go SSD.",
                Price = new Money(2199m, "EUR"),
                Quantity = 20,
                CatalogueId = electronique.Id,
                CreatedBy = "seeder",
                CreatedOn = DateTime.UtcNow
            },

            // --- Products : Vêtements ---
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Veste en cuir noir",
                Description = "Veste en cuir véritable, coupe slim, taille S à XXL.",
                Price = new Money(189m, "EUR"),
                Quantity = 80,
                CatalogueId = vetements.Id,
                CreatedBy = "seeder",
                CreatedOn = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Jean slim stretch",
                Description = "Jean 5 poches, tissu stretch confortable, lavage stone.",
                Price = new Money(59m, "EUR"),
                Quantity = 150,
                CatalogueId = vetements.Id,
                CreatedBy = "seeder",
                CreatedOn = DateTime.UtcNow
            },

            // --- Products : Sport ---
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Nike Air Max 270",
                Description = "Chaussures running légères avec amorti Air Max, semelle réactive.",
                Price = new Money(149m, "EUR"),
                Quantity = 60,
                CatalogueId = sport.Id,
                CreatedBy = "seeder",
                CreatedOn = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Vélo de route Carbon Pro",
                Description = "Cadre carbone T700, groupe Shimano 105, roues 700c.",
                Price = new Money(1499m, "EUR"),
                Quantity = 10,
                CatalogueId = sport.Id,
                CreatedBy = "seeder",
                CreatedOn = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Tapis de yoga antidérapant",
                Description = "Tapis 183x61cm, épaisseur 6mm, matière TPE écologique.",
                Price = new Money(39m, "EUR"),
                Quantity = 200,
                CatalogueId = sport.Id,
                CreatedBy = "seeder",
                CreatedOn = DateTime.UtcNow
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
