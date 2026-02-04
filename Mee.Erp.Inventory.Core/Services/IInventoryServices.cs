using Mee.Erp.Inventory.Core.Entities;
using Mee.Erp.Inventory.Core.Enums;

namespace Mee.Erp.Inventory.Core.Services;

public interface IItemService
{
    Task<IEnumerable<Item>> GetItemsAsync(Guid companyId);
    Task<Item?> GetItemByIdAsync(Guid id);
    Task<Item> CreateItemAsync(Item item);
    Task<Item> UpdateItemAsync(Item item);
    Task DeleteItemAsync(Guid id);
    Task<IEnumerable<Item>> GetLowStockItemsAsync(Guid companyId);
    Task<IEnumerable<Item>> GetItemsByCategoryAsync(Guid companyId, ItemCategory category);
    Task UpdateItemStockLevelsAsync(Guid itemId);
    Task<IEnumerable<Item>> GetReorderSuggestionsAsync(Guid companyId);
}

public interface IWarehouseService
{
    Task<IEnumerable<Warehouse>> GetWarehousesAsync(Guid companyId);
    Task<Warehouse?> GetWarehouseByIdAsync(Guid id);
    Task<Warehouse> CreateWarehouseAsync(Warehouse warehouse);
    Task<Warehouse> UpdateWarehouseAsync(Warehouse warehouse);
    Task DeleteWarehouseAsync(Guid id);
    Task<Warehouse> UpdateWarehouseCapacityAsync(Guid id);
    Task<IEnumerable<WarehouseLocation>> GetWarehouseLocationsAsync(Guid warehouseId);
}

public interface IInventoryTransactionService
{
    Task<IEnumerable<InventoryTransaction>> GetTransactionsAsync(Guid itemId, DateTime? startDate = null, DateTime? endDate = null);
    Task<InventoryTransaction> CreateTransactionAsync(InventoryTransaction transaction);
    Task<IEnumerable<InventoryTransaction>> GetWarehouseTransactionsAsync(Guid warehouseId, DateTime? startDate = null);
    Task<InventoryTransaction> ProcessPurchaseReceiptAsync(PurchaseReceiptDto receipt);
    Task<InventoryTransaction> ProcessSalesIssueAsync(SalesIssueDto issue);
    Task<InventoryTransaction> ProcessTransferAsync(TransferDto transfer);
    Task<IEnumerable<InventoryTransaction>> GetTransactionsByTypeAsync(InventoryTransactionType type);
}

public interface IStockAdjustmentService
{
    Task<IEnumerable<StockAdjustment>> GetAdjustmentsAsync(Guid warehouseId);
    Task<StockAdjustment> CreateAdjustmentAsync(StockAdjustment adjustment);
    Task<StockAdjustment> SubmitForApprovalAsync(Guid id);
    Task<StockAdjustment> ApproveAdjustmentAsync(Guid id, Guid approvedById);
    Task<StockAdjustment> PostAdjustmentAsync(Guid id);
    Task<StockAdjustment> RejectAdjustmentAsync(Guid id, string reason);
    Task<byte[]> GenerateAdjustmentReportAsync(Guid id);
}

public interface IInventoryValuationService
{
    Task<decimal> CalculateInventoryValueAsync(Guid companyId, InventoryValuationMethod method);
    Task<decimal> CalculateItemValueAsync(Guid itemId, InventoryValuationMethod method);
    Task<decimal> CalculateWarehouseValueAsync(Guid warehouseId, InventoryValuationMethod method);
    Task<IEnumerable<ItemValuation>> GetItemValuationsAsync(Guid companyId, InventoryValuationMethod method);
    Task<decimal> CalculateCOGSAsync(Guid itemId, decimal quantity);
    Task<InventoryValuationReport> GenerateValuationReportAsync(Guid companyId, InventoryValuationMethod method);
}

public interface IReorderManagementService
{
    Task<IEnumerable<ReorderSuggestion>> GetReorderSuggestionsAsync(Guid companyId);
    Task<IEnumerable<ReorderSuggestion>> GetWarehouseReorderSuggestionsAsync(Guid warehouseId);
    Task<ReorderPurchaseOrder> GeneratePurchaseOrderAsync(Guid companyId, IEnumerable<Guid> itemIds);
    Task UpdateReorderPointsAsync(Guid itemId, decimal reorderPoint, decimal maxStock);
    Task<IEnumerable<Item>> GetCriticalStockItemsAsync(Guid companyId);
}

public interface ILotTrackingService
{
    Task<IEnumerable<ItemLot>> GetItemLotsAsync(Guid itemId);
    Task<ItemLot> CreateLotAsync(ItemLot lot);
    Task<ItemLot> UpdateLotQuantityAsync(Guid lotId, decimal quantity);
    Task<IEnumerable<ItemLot>> GetExpiringLotsAsync(int daysThreshold);
    Task<ItemLot?> GetLotByNumberAsync(Guid itemId, string lotNumber);
    Task<IEnumerable<ItemLot>> GetAvailableLotsAsync(Guid itemId);
}

public interface ISerialTrackingService
{
    Task<IEnumerable<ItemSerial>> GetItemSerialsAsync(Guid itemId);
    Task<ItemSerial> CreateSerialAsync(ItemSerial serial);
    Task<ItemSerial> UpdateSerialStatusAsync(Guid serialId, SerialStatus status);
    Task<ItemSerial?> GetSerialByNumberAsync(Guid itemId, string serialNumber);
    Task<IEnumerable<ItemSerial>> GetAvailableSerialsAsync(Guid itemId);
    Task<byte[]> GenerateSerialReportAsync(Guid itemId);
}

public interface IInventoryReportingService
{
    Task<byte[]> GenerateInventoryValuationReportAsync(Guid companyId, InventoryValuationMethod method);
    Task<byte[]> GenerateStockStatusReportAsync(Guid companyId);
    Task<byte[]> GenerateWarehouseUtilizationReportAsync(Guid companyId);
    Task<byte[]> GenerateInventoryMovementReportAsync(Guid companyId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateExpiryReportAsync(Guid companyId, int daysThreshold);
    Task<byte[]> GenerateABCAnalysisReportAsync(Guid companyId);
}

public interface IInventoryIntegrationService
{
    Task<IntegrationResult> SyncWithFinanceAsync(InventoryTransaction transaction);
    Task<IntegrationResult> SyncCOGSToFinanceAsync(Guid itemId, decimal quantity, decimal totalCost);
    Task<IntegrationResult> UpdateGLAccountsAsync();
    Task<IntegrationResult> ProcessInventoryWriteOffAsync(Guid itemId, decimal quantity, decimal cost);
}

// DTOs
public class PurchaseReceiptDto
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<string>? SerialNumbers { get; set; }
}

public class SalesIssueDto
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    public decimal Quantity { get; set; }
    public string? SalesOrderNumber { get; set; }
    public string? CustomerReference { get; set; }
    public AllocationMethod AllocationMethod { get; set; }
    public string? LotNumber { get; set; }
    public List<string>? SerialNumbers { get; set; }
}

public class TransferDto
{
    public Guid ItemId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public decimal Quantity { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? LotNumber { get; set; }
    public List<string>? SerialNumbers { get; set; }
}

public class ReorderSuggestion
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal SuggestedQuantity { get; set; }
    public decimal UrgencyLevel { get; set; }
    public DateTime? LastReceiptDate { get; set; }
    public decimal AverageMonthlyUsage { get; set; }
    public decimal UnitCost { get; set; }
    public decimal EstimatedCost { get; set; }
}

public class ReorderPurchaseOrder
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime EstimatedDeliveryDate { get; set; }
    public List<ReorderPurchaseOrderLine> Lines { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class ReorderPurchaseOrderLine
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class ItemValuation
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public InventoryValuationMethod ValuationMethod { get; set; }
    public decimal AverageCost { get; set; }
    public decimal FIFOValue { get; set; }
    public decimal LIFOValue { get; set; }
}

public class InventoryValuationReport
{
    public Guid CompanyId { get; set; }
    public DateTime ReportDate { get; set; }
    public InventoryValuationMethod ValuationMethod { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal RawMaterialsValue { get; set; }
    public decimal WorkInProgressValue { get; set; }
    public decimal FinishedGoodsValue { get; set; }
    public List<ItemValuation> ItemValuations { get; set; } = new();
    public List<WarehouseValuation> WarehouseValuations { get; set; } = new();
}

public class WarehouseValuation
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public int ItemCount { get; set; }
    public decimal UtilizationPercentage { get; set; }
}

public class IntegrationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? JournalEntryId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public enum AllocationMethod
{
    FIFO = 1,
    LIFO = 2,
    Average = 3,
    Specific = 4
}