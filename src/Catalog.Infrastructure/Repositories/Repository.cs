namespace Catalog.Infrastructure.Repositories;

using System.Linq.Expressions;
using Catalog.Domain.Common;
using Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : EntityBase
{
    private readonly CatalogueContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public Repository(CatalogueContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id)
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

    public async Task<IEnumerable<TEntity>> GetAllAsync()
        => await _dbSet.Where(e => !e.IsDeleted).ToListAsync();

    // Fixed: Expression<Func<>> is translated to SQL by EF Core (not in-memory)
    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        => await _dbSet.Where(e => !e.IsDeleted).Where(predicate).ToListAsync();

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = _dbSet.Where(e => !e.IsDeleted);
        return predicate is null
            ? await query.CountAsync()
            : await query.Where(predicate).CountAsync();
    }

    public async Task AddAsync(TEntity entity) => await _dbSet.AddAsync(entity);

    public async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null) _dbSet.Remove(entity);
    }
}
