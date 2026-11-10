using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain.Base;
namespace VocabularyBot.Infrastructure.Persistence.Repositories;

internal sealed class GenericRepository(BotDb context) : IGenericRepository
{
    public IQueryable<T> Query<T>(bool tracking = true) where T : Entity => tracking ? context.Set<T>() : context.Set<T>().AsNoTracking();
    public Task<T?> FindAsync<T>(Guid id, bool tracking = true, CancellationToken cancellationToken = default) where T : Entity => Query<T>(tracking).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    public T? Find<T>(Guid id, bool tracking = true) where T : Entity => Query<T>(tracking).FirstOrDefault(e => e.Id == id);
    public void Create<T>(T entity) where T : Entity => context.Set<T>().Add(entity);
    public async Task CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : Entity => await context.Set<T>().AddAsync(entity, cancellationToken);
    public void Update<T>(T entity) where T : Entity => context.Set<T>().Update(entity);
    public void Delete<T>(T entity) where T : Entity => context.Set<T>().Remove(entity);
    public void SaveChanges() => context.SaveChanges();
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) => await context.SaveChangesAsync(cancellationToken);
    public void ClearChanges() => context.ChangeTracker.Clear();
}
