using Shaml.Domain.Identity;

namespace Shaml.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> ExistsBySocialIdAsync(
        string socialId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<User?> GetBySocialIdAsync(
        string socialId,
        CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);
}