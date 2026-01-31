using Mee.Erp.Finance.Localization.UAE.Domain.Entities;
using Mee.Erp.Shared.Kernel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Localization.UAE.Persistence;

public class UaeDbContext : DbContext
{
    // Define DbSets for UAE-specific entities
    public DbSet<TaxCode> TaxCodes { get; set; }
    public DbSet<TaxCodeRate> TaxCodeRates { get; set; }

    public UaeDbContext(DbContextOptions<UaeDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- AUTOMATIC SOFT-DELETE FILTER ---
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

    // --- AUTOMATIC AUDIT FIELDS ---
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

            if (entityEntry.State == EntityState.Added)
            {
                baseEntity.CreatedDate = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}