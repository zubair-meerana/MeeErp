using Mee.Erp.Shared.Kernel;

namespace Mee.Erp.Inventory.Core.Enums;

public enum InventoryTransactionType
{
    [Display(Name = "Purchase Receipt")]
    PurchaseReceipt = 1,
    
    [Display(Name = "Sales Issue")]
    SalesIssue = 2,
    
    [Display(Name = "Stock Adjustment")]
    StockAdjustment = 3,
    
    [Display(Name = "Transfer")]
    Transfer = 4,
    
    [Display(Name = "Return to Supplier")]
    ReturnToSupplier = 5,
    
    [Display(Name = "Customer Return")]
    CustomerReturn = 6,
    
    [Display(Name = "Production Issue")]
    ProductionIssue = 7,
    
    [Display(Name = "Production Receipt")]
    ProductionReceipt = 8,
    
    [Display(Name = "Scrap")]
    Scrap = 9
}

public enum InventoryValuationMethod
{
    [Display(Name = "First In First Out")]
    FIFO = 1,
    
    [Display(Name = "Last In First Out")]
    LIFO = 2,
    
    [Display(Name = "Weighted Average")]
    WeightedAverage = 3,
    
    [Display(Name = "Specific Cost")]
    SpecificCost = 4,
    
    [Display(Name = "Standard Cost")]
    StandardCost = 5
}

public enum StockStatus
{
    [Display(Name = "In Stock")]
    InStock = 1,
    
    [Display(Name = "Out of Stock")]
    OutOfStock = 2,
    
    [Display(Name = "Low Stock")]
    LowStock = 3,
    
    [Display(Name = "Discontinued")]
    Discontinued = 4,
    
    [Display(Name = "On Order")]
    OnOrder = 5
}

public enum ReorderMethod
{
    [Display(Name = "Manual")]
    Manual = 1,
    
    [Display(Name = "Automatic")]
    Automatic = 2,
    
    [Display(Name = "Min-Max")]
    MinMax = 3,
    
    [Display(Name = "Just in Time")]
    JustInTime = 4
}

public enum WarehouseLocationType
{
    [Display(Name = "Storage")]
    Storage = 1,
    
    [Display(Name = "Picking")]
    Picking = 2,
    
    [Display(Name = "Receiving")]
    Receiving = 3,
    
    [Display(Name = "Shipping")]
    Shipping = 4,
    
    [Display(Name = "Quarantine")]
    Quarantine = 5,
    
    [Display(Name = "Damage")]
    Damage = 6
}

public enum ItemCategory
{
    [Display(Name = "Raw Material")]
    RawMaterial = 1,
    
    [Display(Name = "Finished Goods")]
    FinishedGoods = 2,
    
    [Display(Name = "Work in Progress")]
    WorkInProgress = 3,
    
    [Display(Name = "Consumables")]
    Consumables = 4,
    
    [Display(Name = "Services")]
    Services = 5,
    
    [Display(Name = "Fixed Assets")]
    FixedAssets = 6
}

public enum TrackingMethod
{
    [Display(Name = "None")]
    None = 1,
    
    [Display(Name = "Batch")]
    Batch = 2,
    
    [Display(Name = "Serial")]
    Serial = 3,
    
    [Display(Name = "Both")]
    Both = 4
}