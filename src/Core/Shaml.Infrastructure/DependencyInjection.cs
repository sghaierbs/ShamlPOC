using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Abstractions.Security;
using Shaml.Infrastructure.Persistence;
using Shaml.Infrastructure.Persistence.Messaging;
using Shaml.Infrastructure.Persistence.Repositories;
using Shaml.Infrastructure.Security;

namespace Shaml.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ShamlDatabase")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'ShamlDatabase' is missing.");

        // PostgreSQL
        services.AddDbContext<ShamlDbContext>(options => options.UseNpgsql(connectionString));

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ShamlDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        // Security
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPermissionService, PermissionService>();
        
        // Outbox writer that handle the sequence
        services.AddScoped<OutboxWriter>();

        return services;
    }
}