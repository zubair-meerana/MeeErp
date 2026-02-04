using Mee.Erp.Finance.Core.Entities;
using Mee.Erp.Shared.Kernel;
using Mee.Erp.Inventory.Core.Enums;

namespace Mee.Erp.Inventory.Core.Entities;

public class Item : Entity<Guid>, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ItemCategory Category { get; set; }
    public TrackingMethod TrackingMethod { get; set; }
    public InventoryValuationMethod ValuationMethod { get; set; }
    
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    
    public string? Barcode { get; set; }
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Weight { get; set; }
    public decimal Volume { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    
    // Stock levels
    public decimal OnHandQuantity { get; set; }
    public decimal CommittedQuantity { get; set; }
    public decimal OnOrderQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal MaximumStock { get; set; }
    public ReorderMethod ReorderMethod { get; set; }
    public StockStatus StockStatus { get; set; }
    
    // Financial
    public Guid? InventoryAccountId { get; set; }
    public Account? InventoryAccount { get; set; }
    
    public Guid? CogsAccountId { get; set; }
    public Account? CogsAccount { get; set; }
    
    public Guid? RevenueAccountId { get; set; }
    public Account? RevenueAccount { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsTaxable { get; set; } = true;
    public bool IsSerialized { get; set; }
    public bool IsBatched { get; set; }
    public bool IsPerishable { get; set; }
    public int? ShelfLifeDays { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<ItemWarehouse> ItemWarehouses { get; set; } = new List<ItemWarehouse>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ItemLot> ItemLots { get; set; } = new List<ItemLot>();
    public ICollection<ItemSerial> ItemSerials { get; set; } = new List<ItemSerial>();
}

public class Warehouse : Entity<Guid>, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    
    public Guid? ManagerId { get; set; }
    public User? Manager { get; set; }
    
    public decimal TotalCapacity { get; set; }
    public decimal UsedCapacity { get; set; }
    public decimal AvailableCapacity { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
    public ICollection<ItemWarehouse> ItemWarehouses { get; set; } = new List<ItemWarehouse>();
}

public class WarehouseLocation : Entity<Guid>, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WarehouseLocationType LocationType { get; set; }
    
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    public string Zone { get; set; } = string.Empty;
    public string Aisle { get; set; } = string.Empty;
    public string Bay { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    
    public decimal Capacity { get; set; }
    public decimal UsedCapacity { get; set; }
    public decimal AvailableCapacity { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsPickable { get; set; } = true;
    public bool IsStockable { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ItemLot> ItemLots { get; set; } = new List<ItemLot>();
    public ICollection<ItemSerial> ItemSerials { get; set; } = new List<ItemSerial>();
}

public class ItemWarehouse : Entity<Guid>, ISoftDeletable
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    public decimal OnHandQuantity { get; set; }
    public decimal CommittedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal MaximumStock { get; set; }
    
    public DateTime LastStockUpdate { get; set; }
    public StockStatus StockStatus { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class InventoryTransaction : Entity<Guid>, ISoftDeletable
{
    public string TransactionNumber { get; set; } = string.Empty;
    public InventoryTransactionType TransactionType { get; set; }
    
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    public Guid? LocationId { get; set; }
    public WarehouseLocation? Location { get; set; }
    
    public Guid? FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    
    public Guid? FromLocationId { get; set; }
    public WarehouseLocation? FromLocation { get; set; }
    
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    
    public DateTime TransactionDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    
    // Lot and serial tracking
    public Guid? LotId { get; set; }
    public ItemLot? Lot { get; set; }
    
    public Guid? SerialId { get; set; }
    public ItemSerial? Serial { get; set; }
    
    // Financial integration
    public Guid? JournalEntryId { get; set; }
    public bool IsFinanciallyPosted { get; set; }
    public DateTime? FinancialPostDate { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class ItemLot : Entity<Guid>, ISoftDeletable
{
    public string LotNumber { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public DateTime ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? SupplierLotNumber { get; set; }
    
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    public Guid? LocationId { get; set; }
    public WarehouseLocation? Location { get; set; }
    
    public decimal InitialQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal CommittedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal UnitCost { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsExpired { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}

public class ItemSerial : Entity<Guid>, ISoftDeletable
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? TagNumber { get; set; }
    public DateTime ManufactureDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public DateTime? SaleDate { get; set; }
    
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    public Guid? LocationId { get; set; }
    public WarehouseLocation? Location { get; set; }
    
    public Guid? LotId { get; set; }
    public ItemLot? Lot { get; set; }
    
    public decimal UnitCost { get; set; }
    public SerialStatus Status { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}

public class StockAdjustment : Entity<Guid>, ISoftDeletable
{
    public string AdjustmentNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    public DateTime AdjustmentDate { get; set; }
    public AdjustmentReason Reason { get; set; }
    public string? Notes { get; set; }
    
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    
    public AdjustmentStatus Status { get; set; }
    public Guid? JournalEntryId { get; set; }
    public bool IsFinanciallyPosted { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<StockAdjustmentLine> Lines { get; set; } = new List<StockAdjustmentLine>();
}

public class StockAdjustmentLine : Entity<Guid>, ISoftDeletable
{
    public Guid AdjustmentId { get; set; }
    public StockAdjustment Adjustment { get; set; } = null!;
    
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    
    public Guid? LocationId { get; set; }
    public WarehouseLocation? Location { get; set; }
    
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalAmount { get; set; }
    
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    
    // Navigation properties
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}

// Additional enums
public enum SerialStatus
{
    InStock = 1,
    Committed = 2,
    Shipped = 3,
    Returned = 4,
    Damaged = 5,
    Scrapped = 6
}

public enum AdjustmentReason
{
    PhysicalCount = 1,
    Damage = 2,
    Theft = 3,
    Expiry = 4,
    SystemError = 5,
    Transfer = 6,
    Obsolescence = 7
}

public enum AdjustmentStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Posted = 4,
    Rejected = 5
}