namespace Shaml.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<long> IncrementTokenVersionAsync(
        string userSocialId,
        CancellationToken cancellationToken = default);
}