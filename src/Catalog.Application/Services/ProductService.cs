namespace Catalog.Application.Services;

using AutoMapper;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Cache;
using Catalog.Infrastructure.Repositories;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductRequest request, string userId);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, string userId);
    Task DeleteAsync(Guid id, string userId);
}

public class ProductService : IProductService
{
    private const string Prefix = "products";
    private readonly IRepository<Product> _repo;
    private readonly ICacheService        _cache;
    private readonly IMapper              _mapper;

    public ProductService(IRepository<Product> repo, ICacheService cache, IMapper mapper)
    {
        _repo   = repo;
        _cache  = cache;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize)
    {
        var key    = $"{Prefix}:page:{page}:size:{pageSize}";
        var cached = _cache.Get<PagedResult<ProductDto>>(key);
        if (cached is not null) return cached;

        var (items, total) = await _repo.GetPagedAsync(page, pageSize);
        var result = new PagedResult<ProductDto>(_mapper.Map<IEnumerable<ProductDto>>(items), page, pageSize, total);
        _cache.Set(key, result);
        return result;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var key    = $"{Prefix}:{id}";
        var cached = _cache.Get<ProductDto>(key);
        if (cached is not null) return cached;

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        var dto = _mapper.Map<ProductDto>(entity);
        _cache.Set(key, dto);
        return dto;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, string userId)
    {
        var entity = new Product(
            name:        request.Name,
            price:       new Money(request.Price, request.Currency),
            quantity:    request.Quantity,
            description: request.Description)
        {
            CatalogueId = request.CatalogueId,
            CreatedBy   = userId
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);

        return _mapper.Map<ProductDto>(entity);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, string userId)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Produit {id} non trouve.");

        entity.Name        = request.Name;
        entity.Description = request.Description;
        entity.Price       = new Money(request.Price, request.Currency);
        entity.UpdateStock(request.Quantity, userId);
        entity.MarkAsModified(userId);

        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);

        return _mapper.Map<ProductDto>(entity);
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Produit {id} non trouve.");

        entity.MarkAsDeleted(userId);
        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);
    }
}