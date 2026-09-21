using Findora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Findora.API.Data;

/// <summary>
/// EF Core database context for Findora. Module 1 established the
/// PostgreSQL connection and the shared auditable-entity convention. Module
/// 2 adds the authentication schema (users, roles, refresh/verification/
/// reset tokens). Later feature modules (lost/found reports, matches,
/// claims, etc.) will add their own <c>DbSet</c> properties and entity
/// configurations here.
/// </summary>
public class FindoraDbContext : DbContext
{
    // Fixed seed IDs so the three roles are stable across environments and
    // re-seeding is idempotent (EF Core's HasData diffs against these IDs).
    private static readonly Guid UserRoleId = Guid.Parse("a28d5cb3-28b3-40b8-8108-2c0c92236183");
    private static readonly Guid OrganizationStaffRoleId = Guid.Parse("be6c81f8-1e0f-4790-bbbb-ca929f3c1f10");
    private static readonly Guid AdminRoleId = Guid.Parse("6a52c107-12c0-4ae1-8931-eba522b4a4bf");

    public FindoraDbContext(DbContextOptions<FindoraDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();

            // Module 3 — profile/account-status/reputation.
            entity.Property(u => u.Phone).HasMaxLength(30);
            entity.Property(u => u.AvatarUrl).HasMaxLength(2048);
            entity.Property(u => u.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .HasDefaultValue(UserStatus.Active)
                  .IsRequired();
            entity.Property(u => u.ReputationScore)
                  .HasDefaultValue(0)
                  .IsRequired();

            // Supports admin filtering/moderation queries by status.
            entity.HasIndex(u => u.Status);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.Name).HasMaxLength(50).IsRequired();

            // Seed the three roles Module 2 requires. Fixed IDs (above) keep
            // this idempotent across environments/migrations.
            entity.HasData(
                new Role { Id = UserRoleId, Name = RoleNames.User, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = OrganizationStaffRoleId, Name = RoleNames.OrganizationStaff, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = AdminRoleId, Name = RoleNames.Admin, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.Property(rt => rt.TokenHash).IsRequired();

            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.Property(t => t.TokenHash).IsRequired();

            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.Property(t => t.TokenHash).IsRequired();

            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Domain entity configurations (IEntityTypeConfiguration<T>) will be
        // applied here via modelBuilder.ApplyConfigurationsFromAssembly(...)
        // once Module 4+ introduces concrete entities.
    }

    public override int SaveChanges()
    {
        ApplyAuditConventions();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditConventions();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stamps <see cref="IAuditableEntity.CreatedAt"/>/<see cref="IAuditableEntity.UpdatedAt"/>
    /// automatically so individual services never have to set them by hand.
    /// </summary>
    private void ApplyAuditConventions()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }
    }
}
