using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Shared.Kernel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions; // Required for building the expression
namespace Mee.Erp.Finance.Core.Persistence;

public class FinanceDbContext : DbContext
{
    // We need a DbSet for every entity that should become a table.
// GL Module
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Journal> Journals { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<LedgerEntry> LedgerEntries { get; set; }
    public DbSet<FinancialPeriod> FinancialPeriods { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    // AP Module
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines { get; set; }
    public DbSet<SupplierPayment> SupplierPayments { get; set; }

    // AR Module
    public DbSet<Customer> Customers { get; set; }
    public DbSet<SalesInvoice> SalesInvoices { get; set; }
    public DbSet<SalesInvoiceLine> SalesInvoiceLines { get; set; }
    public DbSet<CustomerPayment> CustomerPayments { get; set; }
    
    // Cash & Bank Module
    public DbSet<BankAccount> BankAccounts { get; set; }
    public DbSet<BankTransaction> BankTransactions { get; set; }
    public DbSet<BankReconciliation> BankReconciliations { get; set; }

    // Fixed Assets Module
    public DbSet<AssetCategory> AssetCategories { get; set; }
    public DbSet<FixedAsset> FixedAssets { get; set; }
public DbSet<FixedAssetDepreciation> FixedAssetDepreciations { get; set; }
    public DbSet<CreditNote> CreditNotes { get; set; }
    public DbSet<CreditNoteLine> CreditNoteLines { get; set; }
    public DbSet<DebitNote> DebitNotes { get; set; }
    public DbSet<DebitNoteLine> DebitNoteLines { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<BudgetLine> BudgetLines { get; set; }

    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Configure Relationships and Constraints ---
        // Example: Ensure all decimal properties are configured for financial precision.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18, 2)"); // Or your desired precision
        }

        // --- AUTOMATIC SOFT-DELETE FILTER ---
        // This is a powerful feature. Every query for any entity inheriting BaseEntity
        // will automatically have a "WHERE IsDeleted = false" clause added.
           // --- CORRECTED AUTOMATIC SOFT-DELETE FILTER ---
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // 1. Create the parameter (e.g., "e") for the lambda expression
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                
                // 2. Access the "IsDeleted" property on the parameter
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                
                // 3. Create the constant value "false"
                var constant = Expression.Constant(false);
                
                // 4. Create the binary expression "e.IsDeleted == false"
                var body = Expression.Equal(property, constant);
                
                // 5. Build the complete lambda expression "e => e.IsDeleted == false"
                var lambda = Expression.Lambda(body, parameter);
                
                // 6. Set the query filter
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
            // TODO: Get the current user's ID and set it here
            // baseEntity.UpdatedBy = _currentUserService.UserId;

            if (entityEntry.State == EntityState.Added)
            {
                baseEntity.CreatedDate = DateTime.UtcNow;
                // TODO: Get the current user's ID and set it here
                // baseEntity.CreatedBy = _currentUserService.UserId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}