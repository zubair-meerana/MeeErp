using Mee.Erp.Inventory.Core.Entities;
using Mee.Erp.Inventory.Core.Enums;
using Mee.Erp.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Mee.Erp.Inventory.Core.Services;

public class ItemService : IItemService
{
    private readonly InventoryDbContext _context;
    private readonly IInventoryTransactionService _transactionService;

    public ItemService(InventoryDbContext context, IInventoryTransactionService transactionService)
    {
        _context = context;
        _transactionService = transactionService;
    }

    public async Task<IEnumerable<Item>> GetItemsAsync(Guid companyId)
    {
        return await _context.Items
            .Include(i => i.Supplier)
            .Include(i => i.InventoryAccount)
            .Include(i => i.CogsAccount)
            .Include(i => i.RevenueAccount)
            .Where(i => i.CompanyId == companyId)
            .OrderBy(i => i.Code)
            .ToListAsync();
    }

    public async Task<Item?> GetItemByIdAsync(Guid id)
    {
        return await _context.Items
            .Include(i => i.Supplier)
            .Include(i => i.InventoryAccount)
            .Include(i => i.CogsAccount)
            .Include(i => i.RevenueAccount)
            .Include(i => i.ItemWarehouses)
                .ThenInclude(iw => iw.Warehouse)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Item> CreateItemAsync(Item item)
    {
        // Generate unique item code if not provided
        if (string.IsNullOrEmpty(item.Code))
        {
            item.Code = await GenerateItemCodeAsync(item.CompanyId);
        }

        // Set default stock status
        item.StockStatus = item.OnHandQuantity > 0 ? StockStatus.InStock : StockStatus.OutOfStock;
        item.AvailableQuantity = item.OnHandQuantity - item.CommittedQuantity;

        item.CreatedDate = DateTime.UtcNow;
        item.UpdatedDate = DateTime.UtcNow;

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Create item warehouse records
        var warehouses = await _context.Warehouses
            .Where(w => w.CompanyId == item.CompanyId && w.IsActive)
            .ToListAsync();

        foreach (var warehouse in warehouses)
        {
            var itemWarehouse = new ItemWarehouse
            {
                ItemId = item.Id,
                WarehouseId = warehouse.Id,
                OnHandQuantity = 0,
                CommittedQuantity = 0,
                AvailableQuantity = 0,
                ReorderPoint = 0,
                MaximumStock = 0,
                StockStatus = StockStatus.OutOfStock,
                LastStockUpdate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.ItemWarehouses.Add(itemWarehouse);
        }

        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<Item> UpdateItemAsync(Item item)
    {
        var existingItem = await _context.Items.FindAsync(item.Id);
        if (existingItem == null)
        {
            throw new InvalidOperationException($"Item with ID {item.Id} not found");
        }

        existingItem.Name = item.Name;
        existingItem.Description = item.Description;
        existingItem.Category = item.Category;
        existingItem.TrackingMethod = item.TrackingMethod;
        existingItem.ValuationMethod = item.ValuationMethod;
        existingItem.SupplierId = item.SupplierId;
        existingItem.Barcode = item.Barcode;
        existingItem.Manufacturer = item.Manufacturer;
        existingItem.PartNumber = item.PartNumber;
        existingItem.UnitCost = item.UnitCost;
        existingItem.UnitPrice = item.UnitPrice;
        existingItem.Weight = item.Weight;
        existingItem.Volume = item.Volume;
        existingItem.UnitOfMeasure = item.UnitOfMeasure;
        existingItem.ReorderPoint = item.ReorderPoint;
        existingItem.MaximumStock = item.MaximumStock;
        existingItem.ReorderMethod = item.ReorderMethod;
        existingItem.InventoryAccountId = item.InventoryAccountId;
        existingItem.CogsAccountId = item.CogsAccountId;
        existingItem.RevenueAccountId = item.RevenueAccountId;
        existingItem.IsActive = item.IsActive;
        existingItem.IsTaxable = item.IsTaxable;
        existingItem.IsSerialized = item.IsSerialized;
        existingItem.IsBatched = item.IsBatched;
        existingItem.IsPerishable = item.IsPerishable;
        existingItem.ShelfLifeDays = item.ShelfLifeDays;
        existingItem.Notes = item.Notes;
        existingItem.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingItem;
    }

    public async Task DeleteItemAsync(Guid id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item != null)
        {
            item.IsDeleted = true;
            item.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Item>> GetLowStockItemsAsync(Guid companyId)
    {
        return await _context.Items
            .Where(i => i.CompanyId == companyId && 
                       i.IsActive && 
                       i.OnHandQuantity <= i.ReorderPoint &&
                       i.ReorderPoint > 0)
            .OrderBy(i => i.Code)
            .ToListAsync();
    }

    public async Task<IEnumerable<Item>> GetItemsByCategoryAsync(Guid companyId, ItemCategory category)
    {
        return await _context.Items
            .Include(i => i.Supplier)
            .Where(i => i.CompanyId == companyId && i.Category == category)
            .OrderBy(i => i.Code)
            .ToListAsync();
    }

    public async Task UpdateItemStockLevelsAsync(Guid itemId)
    {
        var item = await _context.Items
            .Include(i => i.ItemWarehouses)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null) return;

        // Calculate total quantities from all warehouses
        var totalOnHand = item.ItemWarehouses.Sum(iw => iw.OnHandQuantity);
        var totalCommitted = item.ItemWarehouses.Sum(iw => iw.CommittedQuantity);
        var totalAvailable = totalOnHand - totalCommitted;

        item.OnHandQuantity = totalOnHand;
        item.CommittedQuantity = totalCommitted;
        item.AvailableQuantity = totalAvailable;

        // Update stock status
        item.StockStatus = totalOnHand switch
        {
            0 => StockStatus.OutOfStock,
            var q when q <= item.ReorderPoint => StockStatus.LowStock,
            _ => StockStatus.InStock
        };

        item.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Item>> GetReorderSuggestionsAsync(Guid companyId)
    {
        var items = await _context.Items
            .Include(i => i.Supplier)
            .Where(i => i.CompanyId == companyId && 
                       i.IsActive && 
                       i.ReorderMethod != ReorderMethod.Manual)
            .ToListAsync();

        var suggestions = new List<Item>();

        foreach (var item in items)
        {
            var shouldReorder = item.ReorderMethod switch
            {
                ReorderMethod.Automatic => item.AvailableQuantity <= item.ReorderPoint,
                ReorderMethod.MinMax => item.AvailableQuantity <= item.ReorderPoint,
                ReorderMethod.JustInTime => item.AvailableQuantity == 0, // Simplified JIT logic
                _ => false
            };

            if (shouldReorder)
            {
                suggestions.Add(item);
            }
        }

        return suggestions;
    }

    private async Task<string> GenerateItemCodeAsync(Guid companyId)
    {
        var itemCount = await _context.Items
            .CountAsync(i => i.CompanyId == companyId);

        return $"ITM{DateTime.Now:yy}{(itemCount + 1):D4}";
    }
}

public class WarehouseService : IWarehouseService
{
    private readonly InventoryDbContext _context;

    public WarehouseService(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Warehouse>> GetWarehousesAsync(Guid companyId)
    {
        return await _context.Warehouses
            .Include(w => w.Manager)
            .Include(w => w.Locations)
            .Include(w => w.ItemWarehouses)
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.Code)
            .ToListAsync();
    }

    public async Task<Warehouse?> GetWarehouseByIdAsync(Guid id)
    {
        return await _context.Warehouses
            .Include(w => w.Manager)
            .Include(w => w.Locations)
            .Include(w => w.ItemWarehouses)
                .ThenInclude(iw => iw.Item)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<Warehouse> CreateWarehouseAsync(Warehouse warehouse)
    {
        if (string.IsNullOrEmpty(warehouse.Code))
        {
            warehouse.Code = await GenerateWarehouseCodeAsync(warehouse.CompanyId);
        }

        warehouse.UsedCapacity = 0;
        warehouse.AvailableCapacity = warehouse.TotalCapacity;
        warehouse.CreatedDate = DateTime.UtcNow;
        warehouse.UpdatedDate = DateTime.UtcNow;

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return warehouse;
    }

    public async Task<Warehouse> UpdateWarehouseAsync(Warehouse warehouse)
    {
        var existingWarehouse = await _context.Warehouses.FindAsync(warehouse.Id);
        if (existingWarehouse == null)
        {
            throw new InvalidOperationException($"Warehouse with ID {warehouse.Id} not found");
        }

        existingWarehouse.Name = warehouse.Name;
        existingWarehouse.Description = warehouse.Description;
        existingWarehouse.Address = warehouse.Address;
        existingWarehouse.City = warehouse.City;
        existingWarehouse.State = warehouse.State;
        existingWarehouse.Country = warehouse.Country;
        existingWarehouse.PostalCode = warehouse.PostalCode;
        existingWarehouse.ManagerId = warehouse.ManagerId;
        existingWarehouse.TotalCapacity = warehouse.TotalCapacity;
        existingWarehouse.IsActive = warehouse.IsActive;
        existingWarehouse.Notes = warehouse.Notes;
        existingWarehouse.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingWarehouse;
    }

    public async Task DeleteWarehouseAsync(Guid id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse != null)
        {
            warehouse.IsDeleted = true;
            warehouse.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Warehouse> UpdateWarehouseCapacityAsync(Guid id)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.ItemWarehouses)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse == null)
        {
            throw new InvalidOperationException($"Warehouse with ID {id} not found");
        }

        // Calculate used capacity (simplified calculation)
        var totalVolume = warehouse.ItemWarehouses.Sum(iw => iw.OnHandQuantity * 0.1); // Simplified volume calculation
        warehouse.UsedCapacity = totalVolume;
        warehouse.AvailableCapacity = warehouse.TotalCapacity - totalVolume;
        warehouse.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return warehouse;
    }

    public async Task<IEnumerable<WarehouseLocation>> GetWarehouseLocationsAsync(Guid warehouseId)
    {
        return await _context.WarehouseLocations
            .Where(l => l.WarehouseId == warehouseId && l.IsActive)
            .OrderBy(l => l.Zone)
            .ThenBy(l => l.Aisle)
            .ThenBy(l => l.Bay)
            .ThenBy(l => l.Level)
            .ThenBy(l => l.Position)
            .ToListAsync();
    }

    private async Task<string> GenerateWarehouseCodeAsync(Guid companyId)
    {
        var warehouseCount = await _context.Warehouses
            .CountAsync(w => w.CompanyId == companyId);

        return $"WH{DateTime.Now:yy}{(warehouseCount + 1):D2}";
    }
}

public class InventoryTransactionService : IInventoryTransactionService
{
    private readonly InventoryDbContext _context;
    private readonly IItemService _itemService;

    public InventoryTransactionService(InventoryDbContext context, IItemService itemService)
    {
        _context = context;
        _itemService = itemService;
    }

    public async Task<IEnumerable<InventoryTransaction>> GetTransactionsAsync(Guid itemId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Item)
            .Include(t => t.Warehouse)
            .Include(t => t.Location)
            .Where(t => t.ItemId == itemId);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenBy(t => t.TransactionNumber)
            .ToListAsync();
    }

    public async Task<InventoryTransaction> CreateTransactionAsync(InventoryTransaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.TransactionNumber))
        {
            transaction.TransactionNumber = await GenerateTransactionNumberAsync();
        }

        transaction.TotalCost = transaction.Quantity * transaction.UnitCost;
        transaction.CreatedDate = DateTime.UtcNow;
        transaction.UpdatedDate = DateTime.UtcNow;

        _context.InventoryTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Update item stock levels
        await UpdateItemWarehouseStockAsync(transaction);

        return transaction;
    }

    public async Task<IEnumerable<InventoryTransaction>> GetWarehouseTransactionsAsync(Guid warehouseId, DateTime? startDate = null)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Item)
            .Include(t => t.Location)
            .Where(t => t.WarehouseId == warehouseId);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<InventoryTransaction> ProcessPurchaseReceiptAsync(PurchaseReceiptDto receipt)
    {
        var transaction = new InventoryTransaction
        {
            TransactionNumber = await GenerateTransactionNumberAsync(),
            TransactionType = InventoryTransactionType.PurchaseReceipt,
            ItemId = receipt.ItemId,
            WarehouseId = receipt.WarehouseId,
            LocationId = receipt.LocationId,
            Quantity = receipt.Quantity,
            UnitCost = receipt.UnitCost,
            TotalCost = receipt.Quantity * receipt.UnitCost,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = receipt.PurchaseOrderNumber,
            ReferenceType = "PurchaseOrder",
            Notes = "Purchase receipt processed"
        };

        // Handle lot tracking
        if (!string.IsNullOrEmpty(receipt.LotNumber))
        {
            var lot = await CreateOrUpdateLotAsync(receipt);
            transaction.LotId = lot.Id;
        }

        // Handle serial tracking
        if (receipt.SerialNumbers != null && receipt.SerialNumbers.Any())
        {
            foreach (var serialNumber in receipt.SerialNumbers)
            {
                var serial = await CreateSerialAsync(receipt, serialNumber);
                // Create individual transaction for each serial
                var serialTransaction = new InventoryTransaction
                {
                    TransactionNumber = await GenerateTransactionNumberAsync(),
                    TransactionType = InventoryTransactionType.PurchaseReceipt,
                    ItemId = receipt.ItemId,
                    WarehouseId = receipt.WarehouseId,
                    LocationId = receipt.LocationId,
                    Quantity = 1,
                    UnitCost = receipt.UnitCost,
                    TotalCost = receipt.UnitCost,
                    TransactionDate = DateTime.UtcNow,
                    ReferenceNumber = receipt.PurchaseOrderNumber,
                    ReferenceType = "PurchaseOrder",
                    SerialId = serial.Id,
                    LotId = transaction.LotId,
                    Notes = "Serial item receipt"
                };

                await CreateTransactionAsync(serialTransaction);
            }
        }
        else
        {
            await CreateTransactionAsync(transaction);
        }

        return transaction;
    }

    public async Task<InventoryTransaction> ProcessSalesIssueAsync(SalesIssueDto issue)
    {
        // Check stock availability
        var availableQuantity = await GetAvailableQuantityAsync(issue.ItemId, issue.WarehouseId, issue.LocationId);
        if (availableQuantity < issue.Quantity)
        {
            throw new InvalidOperationException($"Insufficient stock. Available: {availableQuantity}, Requested: {issue.Quantity}");
        }

        var transaction = new InventoryTransaction
        {
            TransactionNumber = await GenerateTransactionNumberAsync(),
            TransactionType = InventoryTransactionType.SalesIssue,
            ItemId = issue.ItemId,
            WarehouseId = issue.WarehouseId,
            LocationId = issue.LocationId,
            Quantity = -issue.Quantity, // Negative for issue
            UnitCost = await GetItemCostAsync(issue.ItemId),
            TotalCost = 0, // Will be calculated after unit cost is determined
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = issue.SalesOrderNumber,
            ReferenceType = "SalesOrder",
            Notes = "Sales issue processed"
        };

        transaction.TotalCost = transaction.Quantity * transaction.UnitCost;

        // Handle lot/serial allocation
        if (issue.AllocationMethod == AllocationMethod.Specific)
        {
            if (!string.IsNullOrEmpty(issue.LotNumber))
            {
                var lot = await GetLotByNumberAsync(issue.ItemId, issue.LotNumber);
                if (lot != null)
                {
                    transaction.LotId = lot.Id;
                    await UpdateLotQuantityAsync(lot.Id, -issue.Quantity);
                }
            }
        }

        // Handle serial tracking
        if (issue.SerialNumbers != null && issue.SerialNumbers.Any())
        {
            foreach (var serialNumber in issue.SerialNumbers)
            {
                var serial = await GetSerialByNumberAsync(issue.ItemId, serialNumber);
                if (serial != null && serial.Status == SerialStatus.InStock)
                {
                    await UpdateSerialStatusAsync(serial.Id, SerialStatus.Committed);
                    
                    var serialTransaction = new InventoryTransaction
                    {
                        TransactionNumber = await GenerateTransactionNumberAsync(),
                        TransactionType = InventoryTransactionType.SalesIssue,
                        ItemId = issue.ItemId,
                        WarehouseId = issue.WarehouseId,
                        LocationId = issue.LocationId,
                        Quantity = -1,
                        UnitCost = serial.UnitCost,
                        TotalCost = -serial.UnitCost,
                        TransactionDate = DateTime.UtcNow,
                        ReferenceNumber = issue.SalesOrderNumber,
                        ReferenceType = "SalesOrder",
                        SerialId = serial.Id,
                        Notes = "Serial item issue"
                    };

                    await CreateTransactionAsync(serialTransaction);
                }
            }
        }
        else
        {
            await CreateTransactionAsync(transaction);
        }

        return transaction;
    }

    public async Task<InventoryTransaction> ProcessTransferAsync(TransferDto transfer)
    {
        var availableQuantity = await GetAvailableQuantityAsync(transfer.ItemId, transfer.FromWarehouseId, transfer.FromLocationId);
        if (availableQuantity < transfer.Quantity)
        {
            throw new InvalidOperationException($"Insufficient stock at source warehouse. Available: {availableQuantity}, Requested: {transfer.Quantity}");
        }

        // Create issue transaction from source warehouse
        var issueTransaction = new InventoryTransaction
        {
            TransactionNumber = await GenerateTransactionNumberAsync(),
            TransactionType = InventoryTransactionType.Transfer,
            ItemId = transfer.ItemId,
            WarehouseId = transfer.FromWarehouseId,
            LocationId = transfer.FromLocationId,
            FromWarehouseId = transfer.FromWarehouseId,
            FromLocationId = transfer.FromLocationId,
            Quantity = -transfer.Quantity,
            UnitCost = await GetItemCostAsync(transfer.ItemId),
            TotalCost = -transfer.Quantity * await GetItemCostAsync(transfer.ItemId),
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = transfer.ReferenceNumber,
            ReferenceType = "Transfer",
            Notes = "Transfer - Issue from source warehouse"
        };

        // Create receipt transaction at destination warehouse
        var receiptTransaction = new InventoryTransaction
        {
            TransactionNumber = await GenerateTransactionNumberAsync(),
            TransactionType = InventoryTransactionType.Transfer,
            ItemId = transfer.ItemId,
            WarehouseId = transfer.ToWarehouseId,
            LocationId = transfer.ToLocationId,
            FromWarehouseId = transfer.FromWarehouseId,
            FromLocationId = transfer.FromLocationId,
            Quantity = transfer.Quantity,
            UnitCost = await GetItemCostAsync(transfer.ItemId),
            TotalCost = transfer.Quantity * await GetItemCostAsync(transfer.ItemId),
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = transfer.ReferenceNumber,
            ReferenceType = "Transfer",
            Notes = "Transfer - Receipt at destination warehouse"
        };

        await CreateTransactionAsync(issueTransaction);
        await CreateTransactionAsync(receiptTransaction);

        return receiptTransaction;
    }

    public async Task<IEnumerable<InventoryTransaction>> GetTransactionsByTypeAsync(InventoryTransactionType type)
    {
        return await _context.InventoryTransactions
            .Include(t => t.Item)
            .Include(t => t.Warehouse)
            .Where(t => t.TransactionType == type)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    private async Task<string> GenerateTransactionNumberAsync()
    {
        var transactionCount = await _context.InventoryTransactions.CountAsync();
        return $"TRN{DateTime.Now:yyyyMMddHHmmss}{(transactionCount + 1):D3}";
    }

    private async Task<decimal> GetAvailableQuantityAsync(Guid itemId, Guid warehouseId, Guid? locationId)
    {
        var query = _context.InventoryTransactions
            .Where(t => t.ItemId == itemId && t.WarehouseId == warehouseId);

        if (locationId.HasValue)
        {
            query = query.Where(t => t.LocationId == locationId.Value);
        }

        var quantity = await query.SumAsync(t => t.Quantity);
        return quantity;
    }

    private async Task<decimal> GetItemCostAsync(Guid itemId)
    {
        var item = await _context.Items.FindAsync(itemId);
        return item?.UnitCost ?? 0;
    }

    private async Task UpdateItemWarehouseStockAsync(InventoryTransaction transaction)
    {
        var itemWarehouse = await _context.ItemWarehouses
            .FirstOrDefaultAsync(iw => iw.ItemId == transaction.ItemId && iw.WarehouseId == transaction.WarehouseId);

        if (itemWarehouse != null)
        {
            itemWarehouse.OnHandQuantity += transaction.Quantity;
            itemWarehouse.AvailableQuantity = itemWarehouse.OnHandQuantity - itemWarehouse.CommittedQuantity;
            itemWarehouse.LastStockUpdate = DateTime.UtcNow;
            itemWarehouse.UpdatedDate = DateTime.UtcNow;

            // Update stock status
            itemWarehouse.StockStatus = itemWarehouse.OnHandQuantity switch
            {
                0 => StockStatus.OutOfStock,
                var q when q <= itemWarehouse.ReorderPoint => StockStatus.LowStock,
                _ => StockStatus.InStock
            };
        }

        await _context.SaveChangesAsync();
        await _itemService.UpdateItemStockLevelsAsync(transaction.ItemId);
    }

    private async Task<ItemLot> CreateOrUpdateLotAsync(PurchaseReceiptDto receipt)
    {
        var lot = await _context.ItemLots
            .FirstOrDefaultAsync(l => l.ItemId == receipt.ItemId && 
                                     l.LotNumber == receipt.LotNumber &&
                                     l.WarehouseId == receipt.WarehouseId);

        if (lot == null)
        {
            lot = new ItemLot
            {
                LotNumber = receipt.LotNumber!,
                ItemId = receipt.ItemId,
                WarehouseId = receipt.WarehouseId,
                LocationId = receipt.LocationId,
                ManufactureDate = DateTime.UtcNow,
                ExpiryDate = receipt.ExpiryDate,
                InitialQuantity = receipt.Quantity,
                CurrentQuantity = receipt.Quantity,
                CommittedQuantity = 0,
                AvailableQuantity = receipt.Quantity,
                UnitCost = receipt.UnitCost,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.ItemLots.Add(lot);
        }
        else
        {
            lot.CurrentQuantity += receipt.Quantity;
            lot.AvailableQuantity = lot.CurrentQuantity - lot.CommittedQuantity;
            lot.UpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return lot;
    }

    private async Task<ItemSerial> CreateSerialAsync(PurchaseReceiptDto receipt, string serialNumber)
    {
        var serial = new ItemSerial
        {
            SerialNumber = serialNumber,
            ItemId = receipt.ItemId,
            WarehouseId = receipt.WarehouseId,
            LocationId = receipt.LocationId,
            ManufactureDate = DateTime.UtcNow,
            UnitCost = receipt.UnitCost,
            Status = SerialStatus.InStock,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.ItemSerials.Add(serial);
        await _context.SaveChangesAsync();
        return serial;
    }

    private async Task<ItemLot?> GetLotByNumberAsync(Guid itemId, string lotNumber)
    {
        return await _context.ItemLots
            .FirstOrDefaultAsync(l => l.ItemId == itemId && l.LotNumber == lotNumber);
    }

    private async Task<ItemSerial?> GetSerialByNumberAsync(Guid itemId, string serialNumber)
    {
        return await _context.ItemSerials
            .FirstOrDefaultAsync(s => s.ItemId == itemId && s.SerialNumber == serialNumber);
    }

    private async Task UpdateLotQuantityAsync(Guid lotId, decimal quantity)
    {
        var lot = await _context.ItemLots.FindAsync(lotId);
        if (lot != null)
        {
            lot.CurrentQuantity += quantity;
            lot.AvailableQuantity = lot.CurrentQuantity - lot.CommittedQuantity;
            lot.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task UpdateSerialStatusAsync(Guid serialId, SerialStatus status)
    {
        var serial = await _context.ItemSerials.FindAsync(serialId);
        if (serial != null)
        {
            serial.Status = status;
            serial.UpdatedDate = DateTime.UtcNow;
            if (status == SerialStatus.Shipped)
            {
                serial.SaleDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }
    }
}