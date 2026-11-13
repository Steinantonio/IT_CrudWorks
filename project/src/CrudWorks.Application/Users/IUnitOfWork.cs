namespace CrudWorks.Application.Users;

/// <summary>
/// Commits the unit of work. Repositories stage changes; the use case decides when
/// the transaction boundary closes.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
