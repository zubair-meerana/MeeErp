using Mee.Erp.Core.Domain.Entities;
using Mee.Erp.Shared.Kernel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mee.Erp.Core.Persistence;

public class CoreDbContext : DbContext
{
    // Define a DbSet for each core entity.
    public DbSet<Company> Companies { get; set; }
    public DbSet<BusinessUnit> BusinessUnits { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<User> Users { get; set; }

    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- AUTOMATIC SOFT-DELETE FILTER (Identical to FinanceDbContext) ---
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var constant = Expression.Constant(false);
                var body = Expression.Equal(property, constant);
                var lambda = Expression.Lambda(body, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    // --- AUTOMATIC AUDIT FIELDS (Identical to FinanceDbContext) ---
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var baseEntity = (BaseEntity)entityEntry.Entity;
            baseEntity.UpdatedDate = DateTime.UtcNow;
            // TODO: Get the current user's ID and set it here

            if (entityEntry.State == EntityState.Added)
            {
                baseEntity.CreatedDate = DateTime.UtcNow;
                // TODO: Get the current user's ID and set it here
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}