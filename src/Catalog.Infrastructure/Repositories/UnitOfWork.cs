namespace Catalog.Infrastructure.Repositories;

using Catalog.Domain.Common;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Data;

public interface IUnitOfWork : IDisposable
{
    IRepository<Catalogue> CatalogueRepository { get; }
    IRepository<Product> ProductRepository { get; }
    Task SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogueContext _context;
    private IRepository<Catalogue>? _catalogueRepository;
    private IRepository<Product>? _productRepository;

    public UnitOfWork(CatalogueContext context)
    {
        _context = context;
    }

    public IRepository<Catalogue> CatalogueRepository =>
        _catalogueRepository ??= new Repository<Catalogue>(_context);

    public IRepository<Product> ProductRepository =>
        _productRepository ??= new Repository<Product>(_context);

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}