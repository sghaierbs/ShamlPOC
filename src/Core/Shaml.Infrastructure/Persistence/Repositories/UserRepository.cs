using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Domain.Identity;

namespace Shaml.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(ShamlDbContext dbContext)
    : IUserRepository
{
    public Task<bool> ExistsBySocialIdAsync(
        string socialId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(
            x => x.Id == socialId,
            cancellationToken);
    }

    public Task<bool> ExistsByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(
            x => x.Username == username,
            cancellationToken);
    }

    public Task<User?> GetBySocialIdAsync(
        string socialId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(
            x => x.Id == socialId,
            cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(
            x => x.Username == username,
            cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(
            user,
            cancellationToken);
    }
}