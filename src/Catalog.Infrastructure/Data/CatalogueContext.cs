namespace Catalog.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;

public class CatalogueContext : DbContext
{
    public CatalogueContext(DbContextOptions<CatalogueContext> options) : base(options) { }

    public DbSet<Catalogue> Catalogues => Set<Catalogue>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration Catalogue
        modelBuilder.Entity<Catalogue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.IsDeleted);
        });

        // Configuration Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.CatalogueId);
            entity.HasIndex(e => e.IsDeleted);

            // Configuration du Value Object Money
            entity.Property(p => p.Price)
                .HasConversion(
                    v => v.Amount,
                    v => new Money(v, "EUR"))
                .HasColumnName("Price")
                .HasColumnType("decimal(18,2)");
        });
    }
}