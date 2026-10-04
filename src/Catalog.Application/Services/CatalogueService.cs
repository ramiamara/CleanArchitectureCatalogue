namespace Catalog.Application.Services;

using AutoMapper;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Cache;
using Catalog.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

public interface ICatalogueService
{
    Task<PagedResult<CatalogueDto>> GetAllAsync(int page, int pageSize);
    Task<CatalogueDto?> GetByIdAsync(Guid id);
    Task<CatalogueDto> CreateAsync(CreateCatalogueRequest request, string userId);
    Task<CatalogueDto> UpdateAsync(Guid id, UpdateCatalogueRequest request, string userId);
    Task DeleteAsync(Guid id, string userId);
}

public class CatalogueService : ICatalogueService
{
    private const string Prefix = "catalogues";

    private readonly IRepository<Catalogue> _repo;
    private readonly ICacheService          _cache;
    private readonly IMapper                _mapper;
    private readonly ILogger<CatalogueService> _logger;

    public CatalogueService(IRepository<Catalogue> repo, ICacheService cache, IMapper mapper, ILogger<CatalogueService> logger)
    {
        _repo   = repo;
        _cache  = cache;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<CatalogueDto>> GetAllAsync(int page, int pageSize)
    {
        _logger.LogInformation("Test infos : lecture du catalogue page {Page}", page);
        _logger.LogWarning("Test warnings : lecture du catalogue page {Page}", page);

        var key    = $"{Prefix}:page:{page}:size:{pageSize}";
        var cached = _cache.Get<PagedResult<CatalogueDto>>(key);
        if (cached is not null) return cached;
        int x = 0;
        var res = 5 / x;
        var (items, total) = await _repo.GetPagedAsync(page, pageSize);
        var dtos   = _mapper.Map<IEnumerable<CatalogueDto>>(items);
        var result = new PagedResult<CatalogueDto>(dtos, page, pageSize, total);

        _cache.Set(key, result);
        return result;
    }

    public async Task<CatalogueDto?> GetByIdAsync(Guid id)
    {
        var key    = $"{Prefix}:{id}";
        var cached = _cache.Get<CatalogueDto>(key);
        if (cached is not null) return cached;

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        var dto = _mapper.Map<CatalogueDto>(entity);
        _cache.Set(key, dto);
        return dto;
    }

    public async Task<CatalogueDto> CreateAsync(CreateCatalogueRequest request, string userId)
    {
        var entity = _mapper.Map<Catalogue>(request);
        entity.CreatedBy = userId;

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);

        return _mapper.Map<CatalogueDto>(entity);
    }

    public async Task<CatalogueDto> UpdateAsync(Guid id, UpdateCatalogueRequest request, string userId)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Catalogue {id} non trouve.");

        _mapper.Map(request, entity);
        entity.MarkAsModified(userId);

        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);

        return _mapper.Map<CatalogueDto>(entity);
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Catalogue {id} non trouve.");

        entity.MarkAsDeleted(userId);
        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();
        _cache.RemoveByPrefix(Prefix);
    }
}