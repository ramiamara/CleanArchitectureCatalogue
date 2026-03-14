using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Catalog.Domain.Common;
using Catalog.Infrastructure.Data;

namespace Catalog.Infrastructure.Repositories;

public interface IRepository<TEntity> where TEntity : EntityBase
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, Expression<Func<TEntity, bool>>? predicate = null);
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}

public class Repository<TEntity> : IRepository<TEntity> where TEntity : EntityBase
{
    private readonly CatalogueContext _context;
    private readonly DbSet<TEntity>   _dbSet;

    public Repository(CatalogueContext context)
    {
        _context = context;
        _dbSet   = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id)
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

    public async Task<IEnumerable<TEntity>> GetAllAsync()
        => await _dbSet.Where(e => !e.IsDeleted).ToListAsync();

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        => await _dbSet.Where(e => !e.IsDeleted).Where(predicate).ToListAsync();

    public async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = _dbSet.Where(e => !e.IsDeleted);
        if (predicate is not null) query = query.Where(predicate);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(TEntity entity)    => await _dbSet.AddAsync(entity);
    public Task UpdateAsync(TEntity entity)       { _dbSet.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null) _dbSet.Remove(entity);
    }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}