
using System.Data;
using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Domain.Centers;
using Shaml.Domain.Geography;
using Shaml.Domain.Identity;

namespace Shaml.Infrastructure.Persistence;

public sealed class ShamlDbContext(
    DbContextOptions<ShamlDbContext> options)
    : DbContext(options), IUnitOfWork
{
    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Center> Centers => Set<Center>();

    // Geographic reference data
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();

    public async Task<long> IncrementTokenVersionAsync(
        string userSocialId,
        CancellationToken cancellationToken = default)
    {
        var connection = Database.GetDbConnection();

        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();

            command.CommandText = """
                UPDATE shaml."Users"
                SET "TokenVersion" = "TokenVersion" + 1
                WHERE "SocialId" = @socialId
                  AND "IsActive" = TRUE
                RETURNING "TokenVersion";
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "socialId";
            parameter.Value = userSocialId;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(
                cancellationToken);

            if (result is null || result is DBNull)
            {
                throw new UnauthorizedAccessException(
                    "User is not active or does not exist.");
            }

            return Convert.ToInt64(result);
        }
        finally
        {
            if (shouldClose)
            {
                await Database.CloseConnectionAsync();
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("shaml");

        // =========================================
        // Identity
        // =========================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("SocialId")
                .ValueGeneratedNever();

            entity.Ignore(x => x.SocialId);

            entity.Property(x => x.PublicId)
                .HasColumnName("PublicId");

            entity.Property(x => x.TokenVersion)
                .HasColumnName("TokenVersion")
                .IsRequired()
                .IsConcurrencyToken();

            entity.HasOne<UserRole>()
                .WithMany()
                .HasForeignKey(x => x.DefaultUserRoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);

            // The existing PostgreSQL column stores enum names
            // (Organisation, Center, Public) as varchar values.
            entity.Property(x => x.Scope)
                .HasConversion<string>()
                .IsRequired();
        });

        
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserSocialId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Center>()
                .WithMany()
                .HasForeignKey(x => x.CenterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.CenterId)
                .IsUnique()
                .HasFilter(
                    "\"RoleId\" = 2 AND \"IsActive\" = TRUE AND \"CenterId\" IS NOT NULL");
        });


        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => new
            {
                x.RoleId,
                x.PermissionId
            });
        });

        // =========================================
        // Geographic reference data
        // =========================================

        modelBuilder.Entity<Region>(entity =>
        {
            entity.ToTable("Regions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn();

            entity.Property(x => x.NameAr)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.NameEn)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("Cities");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn();

            entity.Property(x => x.RegionId)
                .IsRequired();

            entity.Property(x => x.NameAr)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.NameEn)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.HasOne<Region>()
                .WithMany()
                .HasForeignKey(x => x.RegionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.RegionId);
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.ToTable("Districts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn();

            entity.Property(x => x.CityId)
                .IsRequired();

            entity.Property(x => x.NameAr)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.NameEn)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.HasOne<City>()
                .WithMany()
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.CityId);
        });
        
        // =========================================
        // Centers
        // =========================================
        modelBuilder.Entity<Center>(entity =>
        {
            entity.ToTable("Centers");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn();

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            // Computed in C#, not stored in PostgreSQL.
            entity.Ignore(x => x.DisplayName);

            entity.Property(x => x.RegionId)
                .IsRequired();

            entity.Property(x => x.CityId)
                .IsRequired();

            entity.Property(x => x.DistrictId);

            entity.Property(x => x.OperationalCapacity)
                .IsRequired();

            entity.Property(x => x.WaitingListLimit)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.Property(x => x.IsReceivingCases)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.HasOne<Region>()
                .WithMany()
                .HasForeignKey(x => x.RegionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<City>()
                .WithMany()
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<District>()
                .WithMany()
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.RegionId);
            entity.HasIndex(x => x.CityId);
        });
    }
}
