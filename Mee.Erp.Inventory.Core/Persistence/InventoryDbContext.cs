using Mee.Erp.Inventory.Core.Entities;
using Mee.Erp.Inventory.Core.Enums;
using Mee.Erp.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Mee.Erp.Inventory.Core.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    public DbSet<ItemWarehouse> ItemWarehouses { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<ItemLot> ItemLots { get; set; }
    public DbSet<ItemSerial> ItemSerials { get; set; }
    public DbSet<StockAdjustment> StockAdjustments { get; set; }
    public DbSet<StockAdjustmentLine> StockAdjustmentLines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Item
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Manufacturer).HasMaxLength(200);
            entity.Property(e => e.PartNumber).HasMaxLength(100);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 4);
            entity.Property(e => e.Weight).HasPrecision(18, 4);
            entity.Property(e => e.Volume).HasPrecision(18, 4);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(20);
            entity.Property(e => e.OnHandQuantity).HasPrecision(18, 4);
            entity.Property(e => e.CommittedQuantity).HasPrecision(18, 4);
            entity.Property(e => e.OnOrderQuantity).HasPrecision(18, 4);
            entity.Property(e => e.AvailableQuantity).HasPrecision(18, 4);
            entity.Property(e => e.ReorderPoint).HasPrecision(18, 4);
            entity.Property(e => e.MaximumStock).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.InventoryAccount)
                .WithMany()
                .HasForeignKey(e => e.InventoryAccountId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.CogsAccount)
                .WithMany()
                .HasForeignKey(e => e.CogsAccountId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.RevenueAccount)
                .WithMany()
                .HasForeignKey(e => e.RevenueAccountId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.CompanyId, e.Code });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Warehouse
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.TotalCapacity).HasPrecision(18, 4);
            entity.Property(e => e.UsedCapacity).HasPrecision(18, 4);
            entity.Property(e => e.AvailableCapacity).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.CompanyId, e.Code });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure WarehouseLocation
        modelBuilder.Entity<WarehouseLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Zone).HasMaxLength(50);
            entity.Property(e => e.Aisle).HasMaxLength(20);
            entity.Property(e => e.Bay).HasMaxLength(20);
            entity.Property(e => e.Level).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(20);
            entity.Property(e => e.Capacity).HasPrecision(18, 4);
            entity.Property(e => e.UsedCapacity).HasPrecision(18, 4);
            entity.Property(e => e.AvailableCapacity).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.Locations)
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.WarehouseId, e.Code });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure ItemWarehouse
        modelBuilder.Entity<ItemWarehouse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OnHandQuantity).HasPrecision(18, 4);
            entity.Property(e => e.CommittedQuantity).HasPrecision(18, 4);
            entity.Property(e => e.AvailableQuantity).HasPrecision(18, 4);
            entity.Property(e => e.ReorderPoint).HasPrecision(18, 4);
            entity.Property(e => e.MaximumStock).HasPrecision(18, 4);
            
            entity.HasOne(e => e.Item)
                .WithMany(i => i.ItemWarehouses)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.ItemWarehouses)
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.ItemId, e.WarehouseId }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure InventoryTransaction
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.ReferenceType).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Item)
                .WithMany(i => i.InventoryTransactions)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Location)
                .WithMany(l => l.Transactions)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.FromWarehouse)
                .WithMany()
                .HasForeignKey(e => e.FromWarehouseId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.FromLocation)
                .WithMany()
                .HasForeignKey(e => e.FromLocationId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Lot)
                .WithMany(l => l.Transactions)
                .HasForeignKey(e => e.LotId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Serial)
                .WithMany(s => s.Transactions)
                .HasForeignKey(e => e.SerialId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.TransactionNumber).IsUnique();
            entity.HasIndex(e => new { e.ItemId, e.TransactionDate });
            entity.HasIndex(e => new { e.WarehouseId, e.TransactionDate });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure ItemLot
        modelBuilder.Entity<ItemLot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LotNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.Property(e => e.SupplierLotNumber).HasMaxLength(100);
            entity.Property(e => e.InitialQuantity).HasPrecision(18, 4);
            entity.Property(e => e.CurrentQuantity).HasPrecision(18, 4);
            entity.Property(e => e.CommittedQuantity).HasPrecision(18, 4);
            entity.Property(e => e.AvailableQuantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Item)
                .WithMany(i => i.ItemLots)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Location)
                .WithMany(l => l.ItemLots)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.ItemId, e.LotNumber });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure ItemSerial
        modelBuilder.Entity<ItemSerial>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TagNumber).HasMaxLength(100);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Item)
                .WithMany(i => i.ItemSerials)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Location)
                .WithMany(l => l.ItemSerials)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Lot)
                .WithMany(l => l.ItemSerials)
                .HasForeignKey(e => e.LotId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.ItemId, e.SerialNumber });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure StockAdjustment
        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdjustmentNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ApprovedBy)
                .WithMany()
                .HasForeignKey(e => e.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.AdjustmentNumber).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure StockAdjustmentLine
        modelBuilder.Entity<StockAdjustmentLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SystemQuantity).HasPrecision(18, 4);
            entity.Property(e => e.CountedQuantity).HasPrecision(18, 4);
            entity.Property(e => e.AdjustmentQuantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Adjustment)
                .WithMany(a => a.Lines)
                .HasForeignKey(e => e.AdjustmentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<ISoftDeletable>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(e => e.CreatedDate).CurrentValue = DateTime.UtcNow;
                entry.Property(e => e.UpdatedDate).CurrentValue = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.UpdatedDate).CurrentValue = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}