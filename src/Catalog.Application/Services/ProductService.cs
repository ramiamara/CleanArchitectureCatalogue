namespace Catalog.Application.Services;

using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Cache;
using Catalog.Infrastructure.Repositories;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProductDto>> GetByCatalogueAsync(Guid catalogueId);
    Task<ProductDto> CreateAsync(CreateProductRequest request, string userId);
    Task<ProductDto> UpdateAsync(Guid id, CreateProductRequest request, string userId);
    Task DeleteAsync(Guid id, string userId);
}

public class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Catalogue> _catalogueRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private const string Prefix = "products";

    public ProductService(IRepository<Product> productRepo, IRepository<Catalogue> catalogueRepo, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _productRepo = productRepo;
        _catalogueRepo = catalogueRepo;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var key = $"{Prefix}:page:{page}:size:{pageSize}";
        var cached = _cache.Get<PagedResult<ProductDto>>(key);
        if (cached is not null) return cached;

        var all = await _productRepo.GetAllAsync();
        var list = all.ToList();
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto);
        var result = new PagedResult<ProductDto>(items, page, pageSize, list.Count);
        _cache.Set(key, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var key = $"{Prefix}:{id}";
        var cached = _cache.Get<ProductDto>(key);
        if (cached is not null) return cached;
        var entity = await _productRepo.GetByIdAsync(id);
        if (entity is null) return null;
        var dto = MapToDto(entity);
        _cache.Set(key, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<IEnumerable<ProductDto>> GetByCatalogueAsync(Guid catalogueId)
    {
        var key = $"{Prefix}:catalogue:{catalogueId}";
        var cached = _cache.Get<IEnumerable<ProductDto>>(key);
        if (cached is not null) return cached;
        var items = await _productRepo.FindAsync(p => p.CatalogueId == catalogueId);
        var dtos = items.Select(MapToDto).ToList();
        _cache.Set(key, dtos, TimeSpan.FromMinutes(5));
        return dtos;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, string userId)
    {
        _ = await _catalogueRepo.GetByIdAsync(request.CatalogueId) ?? throw new KeyNotFoundException($"Catalogue {request.CatalogueId} non trouve.");
        var entity = new Product { Name = request.Name, Description = request.Description, Price = new Money(request.Price, request.Currency), Quantity = request.Quantity, CatalogueId = request.CatalogueId, CreatedBy = userId };
        await _productRepo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);
        return MapToDto(entity);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, CreateProductRequest request, string userId)
    {
        var entity = await _productRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Produit {id} non trouve.");
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Price = new Money(request.Price, request.Currency);
        entity.Quantity = request.Quantity;
        entity.CatalogueId = request.CatalogueId;
        entity.MarkAsModified(userId);
        await _productRepo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var entity = await _productRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Produit {id} non trouve.");
        entity.MarkAsDeleted(userId);
        await _productRepo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);
    }

    private static ProductDto MapToDto(Product p) => new() { Id = p.Id, Name = p.Name, Description = p.Description, Price = p.Price.Amount, Currency = p.Price.Currency, Quantity = p.Quantity, CatalogueId = p.CatalogueId, CreatedOn = p.CreatedOn, CreatedBy = p.CreatedBy, ModifiedOn = p.ModifiedOn, ModifiedBy = p.ModifiedBy };
}
