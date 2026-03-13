namespace Catalog.Application.Services;

using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Cache;
using Catalog.Infrastructure.Repositories;

public interface ICatalogueService
{
    Task<PagedResult<CatalogueDto>> GetAllAsync(int page, int pageSize);
    Task<CatalogueDto?> GetByIdAsync(Guid id);
    Task<CatalogueDto> CreateAsync(CreateCatalogueRequest request, string userId);
    Task<CatalogueDto> UpdateAsync(Guid id, CreateCatalogueRequest request, string userId);
    Task DeleteAsync(Guid id, string userId);
}

public class CatalogueService : ICatalogueService
{
    private readonly IRepository<Catalogue> _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private const string Prefix = "catalogues";

    public CatalogueService(IRepository<Catalogue> repo, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<PagedResult<CatalogueDto>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var key = $"{Prefix}:page:{page}:size:{pageSize}";
        var cached = _cache.Get<PagedResult<CatalogueDto>>(key);
        if (cached is not null) return cached;

        var all = await _repo.GetAllAsync();
        var list = all.ToList();
        var total = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto);
        var result = new PagedResult<CatalogueDto>(items, page, pageSize, total);
        _cache.Set(key, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<CatalogueDto?> GetByIdAsync(Guid id)
    {
        var key = $"{Prefix}:{id}";
        var cached = _cache.Get<CatalogueDto>(key);
        if (cached is not null) return cached;
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        var dto = MapToDto(entity);
        _cache.Set(key, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<CatalogueDto> CreateAsync(CreateCatalogueRequest request, string userId)
    {
        var entity = new Catalogue { Name = request.Name, Description = request.Description, IsActive = true, CreatedBy = userId };
        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);
        return MapToDto(entity);
    }

    public async Task<CatalogueDto> UpdateAsync(Guid id, CreateCatalogueRequest request, string userId)
    {
        var entity = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Catalogue {id} non trouve.");
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.MarkAsModified(userId);
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var entity = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Catalogue {id} non trouve.");
        entity.MarkAsDeleted(userId);
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);
    }

    private static CatalogueDto MapToDto(Catalogue c) => new()
    {
        Id = c.Id, Name = c.Name, Description = c.Description,
        IsActive = c.IsActive, CreatedOn = c.CreatedOn, CreatedBy = c.CreatedBy,
        ModifiedOn = c.ModifiedOn, ModifiedBy = c.ModifiedBy
    };
}
