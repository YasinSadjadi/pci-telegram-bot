using VocabularyBot.Domain.Base;

namespace VocabularyBot.Infrastructure.Persistence.Repositories;

public interface IGenericRepository
{
    IQueryable<T> Query<T>(bool tracking = true) where T : Entity;

    Task<T?> FindAsync<T>(Guid id, bool tracking = true, CancellationToken cancellationToken = default)
        where T : Entity;

    T? Find<T>(Guid id, bool tracking = true) where T : Entity;
    void Create<T>(T entity) where T : Entity;
    Task CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : Entity;
    void Update<T>(T entity) where T : Entity;
    void Delete<T>(T entity) where T : Entity;
    void SaveChanges();
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    void ClearChanges();
}