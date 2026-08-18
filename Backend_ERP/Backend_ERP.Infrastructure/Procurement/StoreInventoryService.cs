using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class StoreInventoryService : IStoreInventoryService
    {
        private readonly ERPDbContext _dbContext;
        private readonly StoreInventoryNumberingService _numberingService;

        public StoreInventoryService(ERPDbContext dbContext, StoreInventoryNumberingService numberingService)
        {
            _dbContext = dbContext;
            _numberingService = numberingService;
        }

        // ── Warehouses ──

        public async Task<List<WarehouseListItemDto>> GetWarehousesAsync(StoreListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.Warehouses.Where(x => !x.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.Code.ToLower().Contains(term) || x.Name.ToLower().Contains(term) || x.Location.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<WarehouseStatus>(query.Status, true, out var status))
            {
                q = q.Where(x => x.Status == status);
            }

            return await q.OrderByDescending(x => x.Id)
                .Select(x => MapWarehouseListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<WarehouseDto?> GetWarehouseByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return item is null ? null : MapWarehouse(item);
        }

        public async Task<WarehouseDto> CreateWarehouseAsync(WarehouseCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            var code = string.IsNullOrWhiteSpace(request.Code)
                ? await _numberingService.GenerateNumberAsync("WH", cancellationToken)
                : request.Code;

            var entity = new Warehouse
            {
                Code = code,
                Name = request.Name,
                Location = request.Location,
                Address = request.Address,
                Manager = request.Manager,
                Capacity = request.Capacity,
                UsedCapacity = 0m,
                AvailableCapacity = request.Capacity,
                Status = request.Status,
                Notes = request.Notes ?? string.Empty,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = currentUser,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Warehouses.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapWarehouse(entity);
        }

        public async Task<WarehouseDto?> UpdateWarehouseAsync(int id, WarehouseCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            entity.Name = request.Name;
            entity.Location = request.Location;
            entity.Address = request.Address;
            entity.Manager = request.Manager;
            entity.Capacity = request.Capacity;
            entity.AvailableCapacity = Math.Max(0m, request.Capacity - entity.UsedCapacity);
            entity.Status = request.Status;
            entity.Notes = request.Notes ?? string.Empty;
            entity.UpdatedBy = currentUser;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapWarehouse(entity);
        }

        public async Task<bool> DeleteWarehouseAsync(int id, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            entity.IsDeleted = true;
            entity.UpdatedBy = currentUser;
            entity.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<WarehouseDashboardDto> GetWarehouseDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.Warehouses.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);

            var totalCapacity = list.Sum(x => x.Capacity);
            var totalUsed = list.Sum(x => x.UsedCapacity);
            var percent = totalCapacity > 0 ? (totalUsed / totalCapacity) * 100m : 0m;

            return new WarehouseDashboardDto
            {
                TotalWarehouses = list.Count,
                CapacityUsagePercent = Math.Round(percent, 2),
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Total Warehouses", Value = list.Count.ToString(), Tone = "default" },
                    new() { Label = "Capacity Usage", Value = $"{percent:F1}%", Tone = percent > 85 ? "danger" : "success" },
                    new() { Label = "Active Warehouses", Value = list.Count(x => x.Status == WarehouseStatus.Active).ToString(), Tone = "success" }
                },
                InventoryDistribution = list.Select(w => new WarehouseDistDto
                {
                    WarehouseName = w.Name,
                    Value = w.UsedCapacity,
                    Count = w.AssignedInventoryCount
                }).ToList()
            };
        }

        // ── Raw Materials ──

        public async Task<List<RawMaterialListItemDto>> GetRawMaterialsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.RawMaterials.Where(x => !x.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.MaterialCode.ToLower().Contains(term) || x.MaterialName.ToLower().Contains(term) || x.Category.ToLower().Contains(term));
            }

            if (query.WarehouseId is > 0)
            {
                q = q.Where(x => x.WarehouseId == query.WarehouseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                q = q.Where(x => x.Category.ToLower() == query.Category.Trim().ToLower());
            }

            return await q.OrderByDescending(x => x.Id)
                .Select(x => MapRawMaterialListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<RawMaterialDto?> GetRawMaterialByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return item is null ? null : MapRawMaterial(item);
        }

        public async Task<RawMaterialDto?> AdjustStockAsync(StockAdjustRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == request.MaterialId && !x.IsDeleted, cancellationToken);
            if (item is null) return null;

            if (request.QuantityDelta < 0)
            {
                var requestedQuantity = Math.Abs(request.QuantityDelta);
                var validationError = StoreInventoryRules.ValidateStockOut(item.AvailableStock, requestedQuantity);
                if (validationError != null)
                {
                    throw new InvalidOperationException(validationError);
                }
            }

            item.AvailableStock += request.QuantityDelta;
            if (item.AvailableStock < 0) item.AvailableStock = 0;
            item.CurrentValue = item.AvailableStock * item.UnitCost;
            item.UpdatedBy = currentUser;
            item.UpdatedAt = DateTime.UtcNow;

            var txnNum = await _numberingService.GenerateNumberAsync("TXN", cancellationToken);
            _dbContext.StockTransactions.Add(new StockTransaction
            {
                TransactionNumber = txnNum,
                TransactionType = StockTxnType.Adjustment,
                MaterialId = item.Id,
                MaterialCode = item.MaterialCode,
                MaterialName = item.MaterialName,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.WarehouseName,
                Quantity = Math.Abs(request.QuantityDelta),
                Unit = item.Unit,
                Reason = request.Reason,
                ReferenceType = StockReferenceType.Manual,
                User = currentUser,
                TransactionDate = DateTime.UtcNow,
                Remarks = request.Remarks ?? string.Empty,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await RecalculateStockAlertsAsync(cancellationToken);
            return MapRawMaterial(item);
        }

        public async Task<List<StockTransactionListItemDto>> GetRawMaterialHistoryAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.StockTransactions
                .Where(x => x.MaterialId == id && !x.IsDeleted)
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => MapStockTxnListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<RawMaterialDashboardDto> GetRawMaterialDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.RawMaterials.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            var low = list.Count(x => x.AvailableStock <= x.ReorderLevel && x.AvailableStock > 0);
            var outStock = list.Count(x => x.AvailableStock <= 0);
            var totalVal = list.Sum(x => x.CurrentValue);

            return new RawMaterialDashboardDto
            {
                TotalMaterials = list.Count,
                LowStock = low,
                OutOfStock = outStock,
                RecentReceipts = list.Count(x => x.LastReceiptDate >= DateTime.UtcNow.AddDays(-7)),
                InventoryValue = totalVal,
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Total Materials", Value = list.Count.ToString(), Tone = "default" },
                    new() { Label = "Low Stock Alerts", Value = low.ToString(), Tone = low > 0 ? "warn" : "default" },
                    new() { Label = "Out of Stock", Value = outStock.ToString(), Tone = outStock > 0 ? "danger" : "default" },
                    new() { Label = "Total Stock Value", Value = $"${totalVal:N2}", Tone = "success" }
                }
            };
        }

        // ── Finished Goods ──

        public async Task<List<FinishedGoodListItemDto>> GetFinishedGoodsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.FinishedGoods.Where(x => !x.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.ProductCode.ToLower().Contains(term) || x.ProductName.ToLower().Contains(term) || x.BatchNumber.ToLower().Contains(term));
            }

            if (query.WarehouseId is > 0)
            {
                q = q.Where(x => x.WarehouseId == query.WarehouseId.Value);
            }

            return await q.OrderByDescending(x => x.Id)
                .Select(x => MapFinishedGoodListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<FinishedGoodDto?> GetFinishedGoodByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.FinishedGoods.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return item is null ? null : MapFinishedGood(item);
        }

        public async Task<FinishedGoodDto?> AdjustFinishedGoodAsync(FinishedGoodAdjustRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.FinishedGoods.FirstOrDefaultAsync(x => x.Id == request.ProductId && !x.IsDeleted, cancellationToken);
            if (item is null) return null;

            if (request.QuantityDelta < 0)
            {
                var requestedQuantity = Math.Abs(request.QuantityDelta);
                var validationError = StoreInventoryRules.ValidateStockOut(item.AvailableQuantity, requestedQuantity);
                if (validationError != null)
                {
                    throw new InvalidOperationException(validationError);
                }
            }

            item.AvailableQuantity += request.QuantityDelta;
            if (item.AvailableQuantity < 0) item.AvailableQuantity = 0;
            item.FinishedQuantity = item.AvailableQuantity + item.ReservedQuantity;
            item.CurrentValue = item.AvailableQuantity * item.UnitCost;
            item.UpdatedBy = currentUser;
            item.UpdatedAt = DateTime.UtcNow;

            var txnNum = await _numberingService.GenerateNumberAsync("TXN", cancellationToken);
            _dbContext.StockTransactions.Add(new StockTransaction
            {
                TransactionNumber = txnNum,
                TransactionType = StockTxnType.Adjustment,
                MaterialId = item.Id,
                MaterialCode = item.ProductCode,
                MaterialName = item.ProductName,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.WarehouseName,
                Quantity = Math.Abs(request.QuantityDelta),
                Unit = item.Unit,
                Reason = request.Reason,
                ReferenceType = StockReferenceType.Manual,
                User = currentUser,
                TransactionDate = DateTime.UtcNow,
                Remarks = request.Remarks ?? string.Empty,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapFinishedGood(item);
        }

        public async Task<List<StockTransactionListItemDto>> GetFinishedGoodHistoryAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.StockTransactions
                .Where(x => x.MaterialId == id && !x.IsDeleted)
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => MapStockTxnListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<FinishedGoodDashboardDto> GetFinishedGoodDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.FinishedGoods.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            var val = list.Sum(x => x.CurrentValue);

            return new FinishedGoodDashboardDto
            {
                AvailableProducts = list.Count(x => x.AvailableQuantity > 0),
                Reserved = (int)list.Sum(x => x.ReservedQuantity),
                ReadyToDispatch = list.Count(x => x.DispatchStatus == FgDispatchStatus.ReadyToDispatch),
                InventoryValue = val,
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Available Products", Value = list.Count(x => x.AvailableQuantity > 0).ToString(), Tone = "success" },
                    new() { Label = "Reserved Stock", Value = list.Sum(x => x.ReservedQuantity).ToString("F0"), Tone = "warn" },
                    new() { Label = "FG Valuation", Value = $"${val:N2}", Tone = "default" }
                }
            };
        }

        // ── Stock Transactions ──

        public async Task<List<StockTransactionListItemDto>> GetStockTransactionsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.StockTransactions.Where(x => !x.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.TransactionNumber.ToLower().Contains(term) || x.MaterialCode.ToLower().Contains(term) || x.MaterialName.ToLower().Contains(term) || x.ReferenceNumber.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.TransactionType) && Enum.TryParse<StockTxnType>(query.TransactionType, true, out var tType))
            {
                q = q.Where(x => x.TransactionType == tType);
            }

            return await q.OrderByDescending(x => x.Id)
                .Select(x => MapStockTxnListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<StockTransactionDto?> GetStockTransactionByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.StockTransactions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return item is null ? null : MapStockTxn(item);
        }

        public async Task<StockTransactionDto> StockInAsync(StockInRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            var mat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == request.MaterialId && !x.IsDeleted, cancellationToken);
            var wh = await _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == request.WarehouseId && !x.IsDeleted, cancellationToken);

            var txnNum = await _numberingService.GenerateNumberAsync("TXN", cancellationToken);

            var entity = new StockTransaction
            {
                TransactionNumber = txnNum,
                TransactionType = StockTxnType.StockIn,
                MaterialId = request.MaterialId,
                MaterialCode = mat?.MaterialCode ?? "MAT-UNKNOWN",
                MaterialName = mat?.MaterialName ?? "Raw Material",
                WarehouseId = request.WarehouseId,
                WarehouseName = wh?.Name ?? "Main Store",
                Quantity = request.Quantity,
                Unit = mat?.Unit ?? "Nos",
                Reason = request.Reason,
                ReferenceType = Enum.TryParse<StockReferenceType>(request.ReferenceType, true, out var refType) ? refType : StockReferenceType.Manual,
                ReferenceNumber = request.ReferenceNumber ?? string.Empty,
                User = currentUser,
                TransactionDate = request.TransactionDate ?? DateTime.UtcNow,
                Remarks = request.Remarks ?? string.Empty,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.StockTransactions.Add(entity);

            if (mat is not null)
            {
                mat.AvailableStock += request.Quantity;
                mat.CurrentValue = mat.AvailableStock * mat.UnitCost;
                mat.LastReceiptDate = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await RecalculateStockAlertsAsync(cancellationToken);
            return MapStockTxn(entity);
        }

        public async Task<StockTransactionDto> StockOutAsync(StockOutRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            var mat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == request.MaterialId && !x.IsDeleted, cancellationToken);
            var wh = await _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == request.WarehouseId && !x.IsDeleted, cancellationToken);

            if (mat is not null)
            {
                var validationError = StoreInventoryRules.ValidateStockOut(mat.AvailableStock, request.Quantity);
                if (validationError != null)
                {
                    throw new InvalidOperationException(validationError);
                }
            }

            var txnNum = await _numberingService.GenerateNumberAsync("TXN", cancellationToken);

            var entity = new StockTransaction
            {
                TransactionNumber = txnNum,
                TransactionType = StockTxnType.StockOut,
                MaterialId = request.MaterialId,
                MaterialCode = mat?.MaterialCode ?? "MAT-UNKNOWN",
                MaterialName = mat?.MaterialName ?? "Raw Material",
                WarehouseId = request.WarehouseId,
                WarehouseName = wh?.Name ?? "Main Store",
                Quantity = request.Quantity,
                Unit = mat?.Unit ?? "Nos",
                Reason = request.Reason,
                ReferenceType = Enum.TryParse<StockReferenceType>(request.ReferenceType, true, out var refType) ? refType : StockReferenceType.Manual,
                ReferenceNumber = request.ReferenceNumber ?? string.Empty,
                User = currentUser,
                TransactionDate = request.TransactionDate ?? DateTime.UtcNow,
                Remarks = request.Remarks ?? string.Empty,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.StockTransactions.Add(entity);

            if (mat is not null)
            {
                mat.AvailableStock = Math.Max(0m, mat.AvailableStock - request.Quantity);
                mat.CurrentValue = mat.AvailableStock * mat.UnitCost;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await RecalculateStockAlertsAsync(cancellationToken);
            return MapStockTxn(entity);
        }

        public async Task<StockTxnDashboardDto> GetStockTxnDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.StockTransactions.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            var today = DateTime.UtcNow.Date;

            return new StockTxnDashboardDto
            {
                TodayTransactions = list.Count(x => x.TransactionDate.Date == today),
                StockIn = list.Count(x => x.TransactionType is StockTxnType.StockIn or StockTxnType.PurchaseReceipt),
                StockOut = list.Count(x => x.TransactionType is StockTxnType.StockOut or StockTxnType.Dispatch or StockTxnType.ProductionConsumption),
                Adjustments = list.Count(x => x.TransactionType == StockTxnType.Adjustment),
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Today's Transactions", Value = list.Count(x => x.TransactionDate.Date == today).ToString(), Tone = "default" },
                    new() { Label = "Stock In Receipts", Value = list.Count(x => x.TransactionType is StockTxnType.StockIn or StockTxnType.PurchaseReceipt).ToString(), Tone = "success" },
                    new() { Label = "Stock Out Dispatches", Value = list.Count(x => x.TransactionType is StockTxnType.StockOut or StockTxnType.Dispatch).ToString(), Tone = "warn" }
                }
            };
        }

        // ── Batches ──

        private static BatchStatus ResolveBatchStatus(InventoryBatch batch)
        {
            if (batch.RemainingQuantity <= 0)
            {
                return BatchStatus.Consumed;
            }

            if (batch.ExpiryDate.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                var expiry = batch.ExpiryDate.Value.Date;

                if (today > expiry)
                {
                    return BatchStatus.Expired;
                }

                if ((expiry - today).TotalDays <= 30)
                {
                    return BatchStatus.ExpiringSoon;
                }
            }

            return BatchStatus.Active;
        }

        private async Task RecalculateBatchStatusesAsync(CancellationToken cancellationToken)
        {
            var batches = await _dbContext.InventoryBatches.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            bool changed = false;

            foreach (var batch in batches)
            {
                batch.RemainingQuantity = Math.Max(0m, batch.AvailableQuantity - batch.ConsumedQuantity);

                var resolved = ResolveBatchStatus(batch);
                if (batch.Status != resolved)
                {
                    batch.Status = resolved;
                    changed = true;
                }
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<List<InventoryBatchListItemDto>> GetBatchesAsync(StoreListQueryDto query, CancellationToken cancellationToken = default)
        {
            await RecalculateBatchStatusesAsync(cancellationToken);

            var q = _dbContext.InventoryBatches.Where(x => !x.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.BatchNumber.ToLower().Contains(term) || x.MaterialCode.ToLower().Contains(term) || x.MaterialName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<BatchStatus>(query.Status, true, out var bStatus))
            {
                q = q.Where(x => x.Status == bStatus);
            }

            return await q.OrderByDescending(x => x.Id)
                .Select(x => MapBatchListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<InventoryBatchDto?> GetBatchByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await RecalculateBatchStatusesAsync(cancellationToken);

            var item = await _dbContext.InventoryBatches.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return item is null ? null : MapBatch(item);
        }

        public async Task<BatchDashboardDto> GetBatchDashboardAsync(CancellationToken cancellationToken = default)
        {
            await RecalculateBatchStatusesAsync(cancellationToken);

            var list = await _dbContext.InventoryBatches.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);

            return new BatchDashboardDto
            {
                ActiveBatches = list.Count(x => x.Status == BatchStatus.Active),
                ExpiringSoon = list.Count(x => x.Status == BatchStatus.ExpiringSoon),
                Expired = list.Count(x => x.Status == BatchStatus.Expired),
                Consumed = list.Count(x => x.Status == BatchStatus.Consumed),
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Active Batches", Value = list.Count(x => x.Status == BatchStatus.Active).ToString(), Tone = "success" },
                    new() { Label = "Expiring Soon", Value = list.Count(x => x.Status == BatchStatus.ExpiringSoon).ToString(), Tone = "warn" },
                    new() { Label = "Expired Batches", Value = list.Count(x => x.Status == BatchStatus.Expired).ToString(), Tone = "danger" }
                }
            };
        }

        // ── Transfers ──

        public async Task<List<StockTransferListItemDto>> GetTransfersAsync(StoreListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.StockTransfers.Include(x => x.Items).Where(x => !x.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.TransferNumber.ToLower().Contains(term) || x.FromWarehouseName.ToLower().Contains(term) || x.ToWarehouseName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<TransferStatus>(query.Status, true, out var trStatus))
            {
                q = q.Where(x => x.Status == trStatus);
            }

            return await q.OrderByDescending(x => x.Id)
                .Select(x => MapTransferListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<StockTransferDto?> GetTransferByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.StockTransfers.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return item is null ? null : MapTransfer(item);
        }

        public async Task<StockTransferDto> TransferAsync(StockTransferCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            if (request.FromWarehouseId == request.ToWarehouseId)
            {
                throw new InvalidOperationException("Source and destination warehouses cannot be the same.");
            }

            var fromWh = await _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == request.FromWarehouseId && !x.IsDeleted, cancellationToken);
            if (fromWh == null || fromWh.Status != WarehouseStatus.Active)
            {
                throw new InvalidOperationException("Source warehouse is inactive or does not exist.");
            }

            var toWh = await _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == request.ToWarehouseId && !x.IsDeleted, cancellationToken);
            if (toWh == null || toWh.Status != WarehouseStatus.Active)
            {
                throw new InvalidOperationException("Destination warehouse is inactive or does not exist.");
            }

            if (request.Items == null || !request.Items.Any())
            {
                throw new InvalidOperationException("Transfer must contain at least one item.");
            }

            var trNum = await _numberingService.GenerateNumberAsync("TRF", cancellationToken);

            var entity = new StockTransfer
            {
                TransferNumber = trNum,
                FromWarehouseId = request.FromWarehouseId,
                FromWarehouseName = fromWh.Name,
                ToWarehouseId = request.ToWarehouseId,
                ToWarehouseName = toWh.Name,
                TransferDate = request.TransferDate,
                Status = TransferStatus.Draft,
                RequestedBy = currentUser,
                Remarks = request.Remarks ?? string.Empty,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = currentUser,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var line in request.Items)
            {
                if (line.Quantity <= 0)
                {
                    throw new InvalidOperationException("Transfer quantity must be greater than zero.");
                }

                var mat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == line.MaterialId && x.WarehouseId == request.FromWarehouseId && !x.IsDeleted, cancellationToken);
                if (mat == null)
                {
                    throw new InvalidOperationException($"Material with ID {line.MaterialId} does not exist in source warehouse.");
                }

                var validationError = StoreInventoryRules.ValidateStockOut(mat.AvailableStock, line.Quantity);
                if (validationError != null)
                {
                    throw new InvalidOperationException(validationError);
                }

                if (!string.IsNullOrWhiteSpace(line.BatchNumber))
                {
                    var batch = await _dbContext.InventoryBatches.FirstOrDefaultAsync(x => x.BatchNumber == line.BatchNumber && x.MaterialId == line.MaterialId && x.WarehouseId == request.FromWarehouseId && !x.IsDeleted, cancellationToken);
                    if (batch == null)
                    {
                        throw new InvalidOperationException($"Batch {line.BatchNumber} does not exist for the material in the source warehouse.");
                    }

                    if (batch.RemainingQuantity < line.Quantity)
                    {
                        throw new InvalidOperationException($"Batch {line.BatchNumber} has insufficient remaining quantity ({batch.RemainingQuantity}) for the requested transfer quantity ({line.Quantity}).");
                    }
                }

                entity.Items.Add(new StockTransferItem
                {
                    MaterialId = line.MaterialId,
                    MaterialCode = mat.MaterialCode,
                    MaterialName = mat.MaterialName,
                    Unit = mat.Unit,
                    Quantity = line.Quantity,
                    BatchNumber = line.BatchNumber
                });
            }

            entity.TotalQuantity = entity.Items.Sum(x => x.Quantity);
            _dbContext.StockTransfers.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapTransfer(entity);
        }

        public async Task<StockTransferDto?> UpdateTransferStatusAsync(int id, StockTransferStatusUpdateDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var entity = await _dbContext.StockTransfers.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
                if (entity is null) return null;

                var oldStatus = entity.Status;
                var newStatus = request.Status;

                if (!StoreInventoryRules.CanTransitionTransfer(oldStatus, newStatus))
                {
                    throw new InvalidOperationException($"Invalid status transition from {oldStatus} to {newStatus}.");
                }

                entity.Status = newStatus;
                if (!string.IsNullOrWhiteSpace(request.Remarks)) entity.Remarks = request.Remarks;
                if (newStatus is TransferStatus.Approved or TransferStatus.Completed) entity.ApprovedBy = currentUser;
                entity.UpdatedBy = currentUser;
                entity.UpdatedAt = DateTime.UtcNow;

                bool deductSource = (oldStatus is TransferStatus.Draft or TransferStatus.Approved) && (newStatus is TransferStatus.Transferred or TransferStatus.Completed);
                bool addDestination = (oldStatus is TransferStatus.Draft or TransferStatus.Approved or TransferStatus.Transferred) && (newStatus == TransferStatus.Completed);
                bool reverseSource = (oldStatus == TransferStatus.Transferred) && (newStatus == TransferStatus.Cancelled);

                if (deductSource)
                {
                    foreach (var item in entity.Items)
                    {
                        var mat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == item.MaterialId && x.WarehouseId == entity.FromWarehouseId && !x.IsDeleted, cancellationToken);
                        if (mat is null)
                        {
                            throw new InvalidOperationException($"Material {item.MaterialId} not found in source warehouse.");
                        }

                        var validationError = StoreInventoryRules.ValidateStockOut(mat.AvailableStock, item.Quantity);
                        if (validationError != null)
                        {
                            throw new InvalidOperationException(validationError);
                        }

                        mat.AvailableStock -= item.Quantity;
                        mat.CurrentValue = mat.AvailableStock * mat.UnitCost;

                        if (!string.IsNullOrWhiteSpace(item.BatchNumber))
                        {
                            var batch = await _dbContext.InventoryBatches.FirstOrDefaultAsync(x => x.BatchNumber == item.BatchNumber && x.MaterialId == item.MaterialId && x.WarehouseId == entity.FromWarehouseId && !x.IsDeleted, cancellationToken);
                            if (batch is not null)
                            {
                                batch.ConsumedQuantity += item.Quantity;
                                batch.RemainingQuantity = Math.Max(0m, batch.AvailableQuantity - batch.ConsumedQuantity);
                                batch.Status = ResolveBatchStatus(batch);
                            }
                        }

                        var txnNum = await _numberingService.GenerateNumberAsync("TXN", cancellationToken);
                        _dbContext.StockTransactions.Add(new StockTransaction
                        {
                            TransactionNumber = txnNum,
                            TransactionType = StockTxnType.TransferOut,
                            MaterialId = item.MaterialId,
                            MaterialCode = item.MaterialCode,
                            MaterialName = item.MaterialName,
                            WarehouseId = entity.FromWarehouseId,
                            WarehouseName = entity.FromWarehouseName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            Reason = $"Stock Transfer Out to {entity.ToWarehouseName}",
                            ReferenceType = StockReferenceType.Transfer,
                            ReferenceNumber = entity.TransferNumber,
                            ReferenceId = entity.Id,
                            User = currentUser,
                            TransactionDate = DateTime.UtcNow,
                            Remarks = request.Remarks ?? string.Empty,
                            CreatedBy = currentUser,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (addDestination)
                {
                    foreach (var item in entity.Items)
                    {
                        var destMat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.MaterialCode == item.MaterialCode && x.WarehouseId == entity.ToWarehouseId && !x.IsDeleted, cancellationToken);
                        if (destMat is null)
                        {
                            var srcMat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == item.MaterialId && !x.IsDeleted, cancellationToken);
                            destMat = new RawMaterial
                            {
                                MaterialCode = item.MaterialCode,
                                MaterialName = item.MaterialName,
                                Category = srcMat?.Category ?? "General",
                                WarehouseId = entity.ToWarehouseId,
                                WarehouseName = entity.ToWarehouseName,
                                Rack = string.Empty,
                                Unit = item.Unit,
                                OpeningStock = 0,
                                AvailableStock = item.Quantity,
                                ReservedStock = 0,
                                MinimumStock = srcMat?.MinimumStock ?? 0,
                                MaximumStock = srcMat?.MaximumStock ?? 0,
                                ReorderLevel = srcMat?.ReorderLevel ?? 0,
                                UnitCost = srcMat?.UnitCost ?? 0,
                                CurrentValue = item.Quantity * (srcMat?.UnitCost ?? 0),
                                BatchCount = !string.IsNullOrWhiteSpace(item.BatchNumber) ? 1 : 0,
                                StockAgeDays = 0,
                                StockAgeBand = StockAgeBand.Band0To30,
                                Supplier = srcMat?.Supplier ?? string.Empty,
                                LastReceiptDate = DateTime.UtcNow,
                                CreatedBy = currentUser,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedBy = currentUser,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _dbContext.RawMaterials.Add(destMat);
                            await _dbContext.SaveChangesAsync(cancellationToken); // Save to get the ID for Batch linkage!
                        }
                        else
                        {
                            destMat.AvailableStock += item.Quantity;
                            destMat.CurrentValue = destMat.AvailableStock * destMat.UnitCost;
                            destMat.LastReceiptDate = DateTime.UtcNow;
                            destMat.UpdatedBy = currentUser;
                            destMat.UpdatedAt = DateTime.UtcNow;
                            if (!string.IsNullOrWhiteSpace(item.BatchNumber))
                            {
                                destMat.BatchCount++;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(item.BatchNumber))
                        {
                            var srcBatch = await _dbContext.InventoryBatches.FirstOrDefaultAsync(x => x.BatchNumber == item.BatchNumber && x.MaterialId == item.MaterialId && x.WarehouseId == entity.FromWarehouseId && !x.IsDeleted, cancellationToken);
                            var destBatch = await _dbContext.InventoryBatches.FirstOrDefaultAsync(x => x.BatchNumber == item.BatchNumber && x.MaterialCode == item.MaterialCode && x.WarehouseId == entity.ToWarehouseId && !x.IsDeleted, cancellationToken);

                            if (destBatch is null)
                            {
                                destBatch = new InventoryBatch
                                {
                                    BatchNumber = item.BatchNumber,
                                    MaterialId = destMat.Id,
                                    MaterialCode = item.MaterialCode,
                                    MaterialName = item.MaterialName,
                                    WarehouseId = entity.ToWarehouseId,
                                    WarehouseName = entity.ToWarehouseName,
                                    Supplier = srcBatch?.Supplier ?? string.Empty,
                                    GRNId = srcBatch?.GRNId,
                                    GRNNumber = srcBatch?.GRNNumber,
                                    ManufacturingDate = srcBatch?.ManufacturingDate ?? DateTime.UtcNow,
                                    ExpiryDate = srcBatch?.ExpiryDate,
                                    AvailableQuantity = item.Quantity,
                                    ConsumedQuantity = 0,
                                    RemainingQuantity = item.Quantity,
                                    Unit = item.Unit,
                                    UnitCost = srcBatch?.UnitCost ?? 0,
                                    Status = BatchStatus.Active,
                                    CreatedBy = currentUser,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedBy = currentUser,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                _dbContext.InventoryBatches.Add(destBatch);
                            }
                            else
                            {
                                destBatch.AvailableQuantity += item.Quantity;
                                destBatch.RemainingQuantity = Math.Max(0m, destBatch.AvailableQuantity - destBatch.ConsumedQuantity);
                                destBatch.Status = ResolveBatchStatus(destBatch);
                                destBatch.UpdatedBy = currentUser;
                                destBatch.UpdatedAt = DateTime.UtcNow;
                            }
                        }

                        var txnNum = await _numberingService.GenerateNumberAsync("TXN", cancellationToken);
                        _dbContext.StockTransactions.Add(new StockTransaction
                        {
                            TransactionNumber = txnNum,
                            TransactionType = StockTxnType.TransferIn,
                            MaterialId = destMat.Id,
                            MaterialCode = item.MaterialCode,
                            MaterialName = item.MaterialName,
                            WarehouseId = entity.ToWarehouseId,
                            WarehouseName = entity.ToWarehouseName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            Reason = $"Stock Transfer In from {entity.FromWarehouseName}",
                            ReferenceType = StockReferenceType.Transfer,
                            ReferenceNumber = entity.TransferNumber,
                            ReferenceId = entity.Id,
                            User = currentUser,
                            TransactionDate = DateTime.UtcNow,
                            Remarks = request.Remarks ?? string.Empty,
                            CreatedBy = currentUser,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (reverseSource)
                {
                    foreach (var item in entity.Items)
                    {
                        var mat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == item.MaterialId && x.WarehouseId == entity.FromWarehouseId && !x.IsDeleted, cancellationToken);
                        if (mat is not null)
                        {
                            mat.AvailableStock += item.Quantity;
                            mat.CurrentValue = mat.AvailableStock * mat.UnitCost;
                        }

                        if (!string.IsNullOrWhiteSpace(item.BatchNumber))
                        {
                            var batch = await _dbContext.InventoryBatches.FirstOrDefaultAsync(x => x.BatchNumber == item.BatchNumber && x.MaterialId == item.MaterialId && x.WarehouseId == entity.FromWarehouseId && !x.IsDeleted, cancellationToken);
                            if (batch is not null)
                            {
                                batch.ConsumedQuantity = Math.Max(0m, batch.ConsumedQuantity - item.Quantity);
                                batch.RemainingQuantity = Math.Max(0m, batch.AvailableQuantity - batch.ConsumedQuantity);
                                batch.Status = ResolveBatchStatus(batch);
                            }
                        }

                        var txnNum = await _numberingService.GenerateNumberAsync("TXN", cancellationToken);
                        _dbContext.StockTransactions.Add(new StockTransaction
                        {
                            TransactionNumber = txnNum,
                            TransactionType = StockTxnType.TransferIn,
                            MaterialId = item.MaterialId,
                            MaterialCode = item.MaterialCode,
                            MaterialName = item.MaterialName,
                            WarehouseId = entity.FromWarehouseId,
                            WarehouseName = entity.FromWarehouseName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            Reason = $"Stock Transfer Cancellation from {entity.ToWarehouseName}",
                            ReferenceType = StockReferenceType.Transfer,
                            ReferenceNumber = entity.TransferNumber,
                            ReferenceId = entity.Id,
                            User = currentUser,
                            TransactionDate = DateTime.UtcNow,
                            Remarks = "Transfer Cancelled Reversal",
                            CreatedBy = currentUser,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await RecalculateStockAlertsAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return MapTransfer(entity);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<TransferDashboardDto> GetTransferDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.StockTransfers.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);

            return new TransferDashboardDto
            {
                PendingTransfers = list.Count(x => x.Status is TransferStatus.Draft or TransferStatus.Approved),
                Completed = list.Count(x => x.Status == TransferStatus.Completed),
                InTransit = list.Count(x => x.Status == TransferStatus.Transferred),
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Pending Transfers", Value = list.Count(x => x.Status is TransferStatus.Draft or TransferStatus.Approved).ToString(), Tone = "warn" },
                    new() { Label = "Completed Transfers", Value = list.Count(x => x.Status == TransferStatus.Completed).ToString(), Tone = "success" },
                    new() { Label = "In-Transit Stock", Value = list.Count(x => x.Status == TransferStatus.Transferred).ToString(), Tone = "default" }
                }
            };
        }

        // ── Alerts ──

        // ── Alerts ──

        private async Task RecalculateStockAlertsAsync(CancellationToken cancellationToken)
        {
            var rawMaterials = await _dbContext.RawMaterials.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            var existingAlerts = await _dbContext.StockAlerts.ToListAsync(cancellationToken);

            var alertsMap = existingAlerts.GroupBy(x => new { x.MaterialId, x.WarehouseId })
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var mat in rawMaterials)
            {
                var key = new { MaterialId = mat.Id, WarehouseId = mat.WarehouseId };
                bool isLowStock = mat.AvailableStock <= mat.MinimumStock;

                if (isLowStock)
                {
                    var priority = mat.AvailableStock <= 0 ? AlertPriority.Critical : AlertPriority.Warning;
                    var reorderQty = Math.Max(0m, mat.MaximumStock - mat.AvailableStock);
                    bool suggestedPurchase = mat.AvailableStock <= mat.ReorderLevel;

                    if (alertsMap.TryGetValue(key, out var alert))
                    {
                        alert.CurrentStock = mat.AvailableStock;
                        alert.MinimumStock = mat.MinimumStock;
                        alert.ReorderQuantity = reorderQty;
                        alert.Priority = priority;
                        alert.SuggestedPurchase = suggestedPurchase;
                        alert.WarehouseName = mat.WarehouseName;
                        alert.MaterialCode = mat.MaterialCode;
                        alert.MaterialName = mat.MaterialName;
                        alert.Unit = mat.Unit;
                    }
                    else
                    {
                        var newAlert = new StockAlert
                        {
                            MaterialId = mat.Id,
                            MaterialCode = mat.MaterialCode,
                            MaterialName = mat.MaterialName,
                            WarehouseId = mat.WarehouseId,
                            WarehouseName = mat.WarehouseName,
                            CurrentStock = mat.AvailableStock,
                            MinimumStock = mat.MinimumStock,
                            ReorderQuantity = reorderQty,
                            Unit = mat.Unit,
                            Priority = priority,
                            SuggestedPurchase = suggestedPurchase,
                            CreatedAt = DateTime.UtcNow
                        };
                        _dbContext.StockAlerts.Add(newAlert);
                    }
                }
                else
                {
                    if (alertsMap.TryGetValue(key, out var alert))
                    {
                        _dbContext.StockAlerts.Remove(alert);
                    }
                }
            }

            var matIds = rawMaterials.Select(x => x.Id).ToHashSet();
            foreach (var alert in existingAlerts)
            {
                if (!matIds.Contains(alert.MaterialId))
                {
                    _dbContext.StockAlerts.Remove(alert);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<StockAlertDto>> GetAlertsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default)
        {
            await RecalculateStockAlertsAsync(cancellationToken);

            var q = _dbContext.StockAlerts.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.MaterialCode.ToLower().Contains(term) || x.MaterialName.ToLower().Contains(term));
            }

            if (query.WarehouseId.HasValue && query.WarehouseId.Value > 0)
            {
                q = q.Where(x => x.WarehouseId == query.WarehouseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Priority) && Enum.TryParse<AlertPriority>(query.Priority, true, out var alertPriority))
            {
                q = q.Where(x => x.Priority == alertPriority);
            }

            return await q.OrderByDescending(x => x.Id)
                .Select(x => MapAlert(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<AlertDashboardDto> GetAlertDashboardAsync(CancellationToken cancellationToken = default)
        {
            await RecalculateStockAlertsAsync(cancellationToken);

            var list = await _dbContext.StockAlerts.ToListAsync(cancellationToken);

            return new AlertDashboardDto
            {
                CriticalAlerts = list.Count(x => x.Priority == AlertPriority.Critical),
                WarningAlerts = list.Count(x => x.Priority == AlertPriority.Warning),
                Normal = list.Count(x => x.Priority == AlertPriority.Normal),
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Critical Stock Alerts", Value = list.Count(x => x.Priority == AlertPriority.Critical).ToString(), Tone = "danger" },
                    new() { Label = "Warning Level Alerts", Value = list.Count(x => x.Priority == AlertPriority.Warning).ToString(), Tone = "warn" }
                }
            };
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "store-inventory.view",
                "store-inventory.create",
                "store-inventory.edit",
                "store-inventory.delete",
                "store-inventory.adjust",
                "store-inventory.transfer",
                "store-inventory.transfer.approve",
                "store-inventory.verify",
                "store-inventory.verify.approve",
                "store-inventory.dashboard.view",
                "store-inventory.valuation.view",
                "store-inventory.alerts.view",
                "store-inventory.export",
                "store-inventory.audit.view"
            ]);

        // ── Valuation ──

        public async Task<StockValuationSummaryDto> GetValuationAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.RawMaterials.Where(x => !x.IsDeleted).AsNoTracking().ToListAsync(cancellationToken);

            var rows = list.Select(m => new StockValuationRowDto
            {
                Id = m.Id.ToString(),
                MaterialId = m.Id,
                MaterialCode = m.MaterialCode,
                MaterialName = m.MaterialName,
                Category = m.Category,
                WarehouseId = m.WarehouseId,
                WarehouseName = m.WarehouseName,
                Quantity = m.AvailableStock,
                Unit = m.Unit,
                AverageCost = m.UnitCost,
                OpeningValue = m.OpeningStock * m.UnitCost,
                ClosingValue = m.CurrentValue,
                CurrentValue = m.CurrentValue
            }).ToList();

            var totalVal = rows.Sum(x => x.CurrentValue);

            return new StockValuationSummaryDto
            {
                InventoryValue = totalVal,
                OpeningValue = rows.Sum(x => x.OpeningValue),
                ClosingValue = totalVal,
                AverageCost = rows.Count > 0 ? rows.Average(x => x.AverageCost) : 0m,
                AsOfDate = DateTime.UtcNow,
                Rows = rows,
                FifoPlaceholder = "FIFO valuation will be calculated by the backend inventory engine.",
                WeightedAveragePlaceholder = "Weighted average cost will be calculated by the backend inventory engine.",
                WarehouseValues = rows.GroupBy(x => new { x.WarehouseId, x.WarehouseName })
                    .Select(g => new WarehouseValueDto { WarehouseId = g.Key.WarehouseId, WarehouseName = g.Key.WarehouseName, Value = g.Sum(v => v.CurrentValue), Quantity = g.Sum(q => q.Quantity) }).ToList(),
                CategoryValues = rows.GroupBy(x => x.Category)
                    .Select(g => new CategoryValueDto { Category = g.Key, Value = g.Sum(v => v.CurrentValue), Quantity = g.Sum(q => q.Quantity) }).ToList()
            };
        }

        // ── Physical Verification ──

        public async Task<List<PhysicalVerificationListItemDto>> GetVerificationsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.PhysicalVerifications.Where(x => !x.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.VerificationNumber.ToLower().Contains(term) || x.Verifier.ToLower().Contains(term) || x.WarehouseName.ToLower().Contains(term));
            }

            return await q.OrderByDescending(x => x.Id)
                .Select(x => MapVerificationListItem(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<PhysicalVerificationDto?> GetVerificationByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.PhysicalVerifications.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return item is null ? null : MapVerification(item);
        }

        private static List<InventoryTimelineEventDto> BuildVerificationTimeline(PhysicalVerification entity)
        {
            var list = new List<InventoryTimelineEventDto>
            {
                new()
                {
                    Id = $"pv-tl-1-{entity.Id}",
                    Date = entity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = entity.CreatedBy,
                    Action = "Scheduled",
                    ToStatus = "Scheduled"
                }
            };

            if (entity.Status != VerificationStatus.Scheduled)
            {
                list.Insert(0, new InventoryTimelineEventDto
                {
                    Id = $"pv-tl-2-{entity.Id}",
                    Date = entity.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = entity.UpdatedBy,
                    Action = $"Status → {entity.Status}",
                    FromStatus = "Scheduled",
                    ToStatus = entity.Status.ToString(),
                    Remarks = entity.Remarks
                });
            }

            return list;
        }

        public async Task<PhysicalVerificationDto> VerifyStockAsync(PhysicalVerificationCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Verifier))
            {
                throw new InvalidOperationException("Verifier is required.");
            }

            if (request.Lines == null || !request.Lines.Any())
            {
                throw new InvalidOperationException("At least one verification line is required.");
            }

            var materialIds = request.Lines.Select(x => x.MaterialId).ToList();
            if (materialIds.Distinct().Count() != request.Lines.Count)
            {
                throw new InvalidOperationException("Duplicate materials are not allowed on one verification.");
            }

            var wh = await _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == request.WarehouseId && !x.IsDeleted, cancellationToken);
            if (wh == null || wh.Status != WarehouseStatus.Active)
            {
                throw new InvalidOperationException("Selected warehouse is inactive or does not exist.");
            }

            var pvNum = await _numberingService.GenerateNumberAsync("PV", cancellationToken);

            var entity = new PhysicalVerification
            {
                VerificationNumber = pvNum,
                WarehouseId = request.WarehouseId,
                WarehouseName = wh.Name,
                Verifier = request.Verifier.Trim(),
                VerificationDate = request.VerificationDate,
                Status = VerificationStatus.Scheduled,
                Remarks = request.Remarks ?? string.Empty,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = currentUser,
                UpdatedAt = DateTime.UtcNow
            };

            decimal totalExpected = 0;
            decimal totalActual = 0;
            decimal totalVariance = 0;
            decimal totalVarianceValue = 0;

            foreach (var line in request.Lines)
            {
                if (line.ExpectedQuantity < 0 || line.ActualQuantity < 0)
                {
                    throw new InvalidOperationException("Quantities cannot be negative.");
                }

                if (line.ExpectedQuantity != line.ActualQuantity && string.IsNullOrWhiteSpace(line.Remarks))
                {
                    throw new InvalidOperationException("Line remarks are required when expected and actual quantities differ.");
                }

                var mat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == line.MaterialId && x.WarehouseId == request.WarehouseId && !x.IsDeleted, cancellationToken);
                if (mat == null)
                {
                    throw new InvalidOperationException($"Material with ID {line.MaterialId} does not exist in the selected warehouse.");
                }

                var lineVariance = line.ActualQuantity - line.ExpectedQuantity;
                var lineVarianceValue = lineVariance * mat.UnitCost;

                totalExpected += line.ExpectedQuantity;
                totalActual += line.ActualQuantity;
                totalVariance += lineVariance;
                totalVarianceValue += lineVarianceValue;

                entity.Lines.Add(new PhysicalVerificationLine
                {
                    MaterialId = line.MaterialId,
                    MaterialCode = mat.MaterialCode,
                    MaterialName = mat.MaterialName,
                    Unit = mat.Unit,
                    ExpectedQuantity = line.ExpectedQuantity,
                    ActualQuantity = line.ActualQuantity,
                    Variance = lineVariance,
                    Remarks = line.Remarks ?? string.Empty
                });
            }

            entity.ExpectedQuantity = totalExpected;
            entity.ActualQuantity = totalActual;
            entity.Variance = totalVariance;
            entity.VarianceValue = totalVarianceValue;

            _dbContext.PhysicalVerifications.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapVerification(entity);
        }

        public async Task<PhysicalVerificationDto?> UpdateVerificationStatusAsync(int id, PhysicalVerificationStatusUpdateDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var entity = await _dbContext.PhysicalVerifications.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
                if (entity is null) return null;

                var oldStatus = entity.Status;
                var newStatus = request.Status;

                if (!StoreInventoryRules.CanTransitionVerification(oldStatus, newStatus))
                {
                    throw new InvalidOperationException($"Invalid status transition from {oldStatus} to {newStatus}.");
                }

                if ((newStatus == VerificationStatus.Cancelled || newStatus == VerificationStatus.Adjusted) && string.IsNullOrWhiteSpace(request.Remarks))
                {
                    throw new InvalidOperationException("Remarks are required for this action.");
                }

                entity.Status = newStatus;
                if (!string.IsNullOrWhiteSpace(request.Remarks)) entity.Remarks = request.Remarks;
                if (newStatus == VerificationStatus.Adjusted)
                {
                    entity.ApprovedBy = currentUser;
                    entity.AdjustmentPosted = true;
                }
                entity.UpdatedBy = currentUser;
                entity.UpdatedAt = DateTime.UtcNow;

                if (newStatus == VerificationStatus.Adjusted && oldStatus != VerificationStatus.Adjusted)
                {
                    foreach (var line in entity.Lines)
                    {
                        if (line.Variance == 0) continue;

                        var mat = await _dbContext.RawMaterials.FirstOrDefaultAsync(x => x.Id == line.MaterialId && x.WarehouseId == entity.WarehouseId && !x.IsDeleted, cancellationToken);
                        if (mat is null)
                        {
                            throw new InvalidOperationException($"Material {line.MaterialId} not found in warehouse.");
                        }

                        var nextStock = mat.AvailableStock + line.Variance;
                        if (nextStock < 0)
                        {
                            throw new InvalidOperationException($"Adjustment would make {mat.MaterialCode} negative. Available: {mat.AvailableStock}");
                        }

                        mat.AvailableStock = nextStock;
                        mat.CurrentValue = mat.AvailableStock * mat.UnitCost;
                        mat.UpdatedBy = currentUser;
                        mat.UpdatedAt = DateTime.UtcNow;

                        var batches = await _dbContext.InventoryBatches.Where(x => x.MaterialId == mat.Id && x.WarehouseId == entity.WarehouseId && !x.IsDeleted)
                            .OrderByDescending(x => x.Id)
                            .ToListAsync(cancellationToken);
                        if (batches.Any())
                        {
                            var targetBatch = batches.First();
                            if (line.Variance > 0)
                            {
                                targetBatch.AvailableQuantity += line.Variance;
                                targetBatch.RemainingQuantity = Math.Max(0m, targetBatch.AvailableQuantity - targetBatch.ConsumedQuantity);
                                targetBatch.Status = ResolveBatchStatus(targetBatch);
                            }
                            else
                            {
                                targetBatch.ConsumedQuantity += Math.Abs(line.Variance);
                                targetBatch.RemainingQuantity = Math.Max(0m, targetBatch.AvailableQuantity - targetBatch.ConsumedQuantity);
                                targetBatch.Status = ResolveBatchStatus(targetBatch);
                            }
                        }

                        var txnNum = await _numberingService.GenerateNumberAsync("TXN", cancellationToken);
                        _dbContext.StockTransactions.Add(new StockTransaction
                        {
                            TransactionNumber = txnNum,
                            TransactionType = StockTxnType.Adjustment,
                            MaterialId = line.MaterialId,
                            MaterialCode = line.MaterialCode,
                            MaterialName = line.MaterialName,
                            WarehouseId = entity.WarehouseId,
                            WarehouseName = entity.WarehouseName,
                            Quantity = Math.Abs(line.Variance),
                            Unit = line.Unit,
                            Reason = "Physical Verification count reconciliation",
                            ReferenceType = StockReferenceType.Verification,
                            ReferenceNumber = entity.VerificationNumber,
                            ReferenceId = entity.Id,
                            User = currentUser,
                            TransactionDate = DateTime.UtcNow,
                            Remarks = request.Remarks ?? string.Empty,
                            CreatedBy = currentUser,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                if (newStatus == VerificationStatus.Adjusted)
                {
                    await RecalculateStockAlertsAsync(cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);

                return MapVerification(entity);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<VerificationDashboardDto> GetVerificationDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.PhysicalVerifications.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);

            return new VerificationDashboardDto
            {
                PendingVerifications = list.Count(x => x.Status is VerificationStatus.Scheduled or VerificationStatus.InProgress),
                Completed = list.Count(x => x.Status is VerificationStatus.Completed or VerificationStatus.Adjusted),
                VarianceAmount = list.Sum(x => x.VarianceValue),
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Pending Verifications", Value = list.Count(x => x.Status is VerificationStatus.Scheduled or VerificationStatus.InProgress).ToString(), Tone = "warn" },
                    new() { Label = "Completed Verifications", Value = list.Count(x => x.Status is VerificationStatus.Completed or VerificationStatus.Adjusted).ToString(), Tone = "success" },
                    new() { Label = "Variance Amount", Value = $"${list.Sum(x => x.VarianceValue):N2}", Tone = "danger" }
                }
            };
        }

        // ── Mapping Helpers ──

        private static WarehouseListItemDto MapWarehouseListItem(Warehouse entity) => new()
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Location = entity.Location,
            Manager = entity.Manager,
            Capacity = entity.Capacity,
            UsedCapacity = entity.UsedCapacity,
            AvailableCapacity = entity.AvailableCapacity,
            Status = entity.Status,
            AssignedInventoryCount = entity.AssignedInventoryCount
        };

        private static WarehouseDto MapWarehouse(Warehouse entity) => new()
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Location = entity.Location,
            Address = entity.Address,
            Manager = entity.Manager,
            Capacity = entity.Capacity,
            UsedCapacity = entity.UsedCapacity,
            AvailableCapacity = entity.AvailableCapacity,
            Status = entity.Status,
            AssignedInventoryCount = entity.AssignedInventoryCount,
            Notes = entity.Notes,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt
        };

        private static RawMaterialListItemDto MapRawMaterialListItem(RawMaterial entity) => new()
        {
            Id = entity.Id,
            MaterialCode = entity.MaterialCode,
            MaterialName = entity.MaterialName,
            Category = entity.Category,
            WarehouseName = entity.WarehouseName,
            Unit = entity.Unit,
            AvailableStock = entity.AvailableStock,
            ReservedStock = entity.ReservedStock,
            MinimumStock = entity.MinimumStock,
            ReorderLevel = entity.ReorderLevel,
            CurrentValue = entity.CurrentValue,
            StockAgeBand = entity.StockAgeBand,
            Supplier = entity.Supplier,
            LastReceiptDate = entity.LastReceiptDate,
            LinkedGRNNumber = entity.LinkedGRNNumber,
            IsLowStock = entity.AvailableStock <= entity.ReorderLevel,
            IsOutOfStock = entity.AvailableStock <= 0
        };

        private static RawMaterialDto MapRawMaterial(RawMaterial entity) => new()
        {
            Id = entity.Id,
            MaterialCode = entity.MaterialCode,
            MaterialName = entity.MaterialName,
            Category = entity.Category,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.WarehouseName,
            Rack = entity.Rack,
            Unit = entity.Unit,
            OpeningStock = entity.OpeningStock,
            AvailableStock = entity.AvailableStock,
            ReservedStock = entity.ReservedStock,
            MinimumStock = entity.MinimumStock,
            MaximumStock = entity.MaximumStock,
            ReorderLevel = entity.ReorderLevel,
            UnitCost = entity.UnitCost,
            CurrentValue = entity.CurrentValue,
            BatchCount = entity.BatchCount,
            StockAgeDays = entity.StockAgeDays,
            StockAgeBand = entity.StockAgeBand,
            Supplier = entity.Supplier,
            LastReceiptDate = entity.LastReceiptDate,
            LinkedGRNId = entity.LinkedGRNId,
            LinkedGRNNumber = entity.LinkedGRNNumber,
            LinkedPOId = entity.LinkedPOId,
            LinkedPONumber = entity.LinkedPONumber,
            Notes = entity.Notes,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt
        };

        private static FinishedGoodListItemDto MapFinishedGoodListItem(FinishedGood entity) => new()
        {
            Id = entity.Id,
            ProductCode = entity.ProductCode,
            ProductName = entity.ProductName,
            FinishedQuantity = entity.FinishedQuantity,
            ReservedQuantity = entity.ReservedQuantity,
            AvailableQuantity = entity.AvailableQuantity,
            WarehouseName = entity.WarehouseName,
            BatchNumber = entity.BatchNumber,
            ProductionReference = entity.ProductionReference,
            ManufacturingDate = entity.ManufacturingDate,
            ExpiryDate = entity.ExpiryDate,
            CurrentValue = entity.CurrentValue,
            DispatchStatus = entity.DispatchStatus
        };

        private static FinishedGoodDto MapFinishedGood(FinishedGood entity) => new()
        {
            Id = entity.Id,
            ProductCode = entity.ProductCode,
            ProductName = entity.ProductName,
            FinishedQuantity = entity.FinishedQuantity,
            ReservedQuantity = entity.ReservedQuantity,
            AvailableQuantity = entity.AvailableQuantity,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.WarehouseName,
            BatchNumber = entity.BatchNumber,
            ProductionReference = entity.ProductionReference,
            ManufacturingDate = entity.ManufacturingDate,
            ExpiryDate = entity.ExpiryDate,
            UnitCost = entity.UnitCost,
            CurrentValue = entity.CurrentValue,
            DispatchStatus = entity.DispatchStatus,
            Unit = entity.Unit,
            Notes = entity.Notes,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt
        };

        private static StockTransactionListItemDto MapStockTxnListItem(StockTransaction entity) => new()
        {
            Id = entity.Id,
            TransactionNumber = entity.TransactionNumber,
            TransactionType = entity.TransactionType,
            MaterialCode = entity.MaterialCode,
            MaterialName = entity.MaterialName,
            WarehouseName = entity.WarehouseName,
            Quantity = entity.Quantity,
            Unit = entity.Unit,
            Reason = entity.Reason,
            ReferenceType = entity.ReferenceType,
            ReferenceNumber = entity.ReferenceNumber,
            User = entity.User,
            TransactionDate = entity.TransactionDate
        };

        private static StockTransactionDto MapStockTxn(StockTransaction entity) => new()
        {
            Id = entity.Id,
            TransactionNumber = entity.TransactionNumber,
            TransactionType = entity.TransactionType,
            MaterialId = entity.MaterialId,
            MaterialCode = entity.MaterialCode,
            MaterialName = entity.MaterialName,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.WarehouseName,
            Quantity = entity.Quantity,
            Unit = entity.Unit,
            Reason = entity.Reason,
            ReferenceType = entity.ReferenceType,
            ReferenceNumber = entity.ReferenceNumber,
            ReferenceId = entity.ReferenceId,
            User = entity.User,
            TransactionDate = entity.TransactionDate,
            Remarks = entity.Remarks,
            Notes = entity.Notes,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt
        };

        private static InventoryBatchListItemDto MapBatchListItem(InventoryBatch entity) => new()
        {
            Id = entity.Id,
            BatchNumber = entity.BatchNumber,
            MaterialCode = entity.MaterialCode,
            MaterialName = entity.MaterialName,
            WarehouseName = entity.WarehouseName,
            Supplier = entity.Supplier,
            GRNNumber = entity.GRNNumber,
            ManufacturingDate = entity.ManufacturingDate,
            ExpiryDate = entity.ExpiryDate,
            AvailableQuantity = entity.AvailableQuantity,
            ConsumedQuantity = entity.ConsumedQuantity,
            RemainingQuantity = entity.RemainingQuantity,
            Unit = entity.Unit,
            Status = entity.Status
        };

        private static InventoryBatchDto MapBatch(InventoryBatch entity) => new()
        {
            Id = entity.Id,
            BatchNumber = entity.BatchNumber,
            MaterialId = entity.MaterialId,
            MaterialCode = entity.MaterialCode,
            MaterialName = entity.MaterialName,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.WarehouseName,
            Supplier = entity.Supplier,
            GRNId = entity.GRNId,
            GRNNumber = entity.GRNNumber,
            ManufacturingDate = entity.ManufacturingDate,
            ExpiryDate = entity.ExpiryDate,
            AvailableQuantity = entity.AvailableQuantity,
            ConsumedQuantity = entity.ConsumedQuantity,
            RemainingQuantity = entity.RemainingQuantity,
            Unit = entity.Unit,
            UnitCost = entity.UnitCost,
            Status = entity.Status,
            Notes = entity.Notes,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt
        };

        private static StockTransferListItemDto MapTransferListItem(StockTransfer entity) => new()
        {
            Id = entity.Id,
            TransferNumber = entity.TransferNumber,
            FromWarehouseName = entity.FromWarehouseName,
            ToWarehouseName = entity.ToWarehouseName,
            ItemCount = entity.Items.Count,
            TotalQuantity = entity.TotalQuantity,
            Status = entity.Status,
            TransferDate = entity.TransferDate,
            RequestedBy = entity.RequestedBy,
            ApprovedBy = entity.ApprovedBy
        };

        private static StockTransferDto MapTransfer(StockTransfer entity) => new()
        {
            Id = entity.Id,
            TransferNumber = entity.TransferNumber,
            FromWarehouseId = entity.FromWarehouseId,
            FromWarehouseName = entity.FromWarehouseName,
            ToWarehouseId = entity.ToWarehouseId,
            ToWarehouseName = entity.ToWarehouseName,
            TotalQuantity = entity.TotalQuantity,
            Status = entity.Status,
            TransferDate = entity.TransferDate,
            RequestedBy = entity.RequestedBy,
            ApprovedBy = entity.ApprovedBy,
            Remarks = entity.Remarks,
            Notes = entity.Notes,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt,
            Items = entity.Items.Select(x => new StockTransferItemDto
            {
                Id = x.Id.ToString(),
                MaterialId = x.MaterialId,
                MaterialCode = x.MaterialCode,
                MaterialName = x.MaterialName,
                Unit = x.Unit,
                Quantity = x.Quantity,
                BatchNumber = x.BatchNumber
            }).ToList()
        };

        private static StockAlertDto MapAlert(StockAlert entity) => new()
        {
            Id = entity.Id,
            MaterialId = entity.MaterialId,
            MaterialCode = entity.MaterialCode,
            MaterialName = entity.MaterialName,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.WarehouseName,
            CurrentStock = entity.CurrentStock,
            MinimumStock = entity.MinimumStock,
            ReorderQuantity = entity.ReorderQuantity,
            Unit = entity.Unit,
            Priority = entity.Priority,
            SuggestedPurchase = entity.SuggestedPurchase,
            LinkedRequisitionId = entity.LinkedRequisitionId,
            LinkedRequisitionNumber = entity.LinkedRequisitionNumber,
            CreatedAt = entity.CreatedAt
        };

        private static PhysicalVerificationListItemDto MapVerificationListItem(PhysicalVerification entity) => new()
        {
            Id = entity.Id,
            VerificationNumber = entity.VerificationNumber,
            WarehouseName = entity.WarehouseName,
            Verifier = entity.Verifier,
            VerificationDate = entity.VerificationDate,
            ExpectedQuantity = entity.ExpectedQuantity,
            ActualQuantity = entity.ActualQuantity,
            Variance = entity.Variance,
            VarianceValue = entity.VarianceValue,
            Status = entity.Status,
            AdjustmentPosted = entity.AdjustmentPosted
        };

        private static PhysicalVerificationDto MapVerification(PhysicalVerification entity) => new()
        {
            Id = entity.Id,
            VerificationNumber = entity.VerificationNumber,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.WarehouseName,
            Verifier = entity.Verifier,
            VerificationDate = entity.VerificationDate,
            ExpectedQuantity = entity.ExpectedQuantity,
            ActualQuantity = entity.ActualQuantity,
            Variance = entity.Variance,
            VarianceValue = entity.VarianceValue,
            Status = entity.Status,
            AdjustmentPosted = entity.AdjustmentPosted,
            Remarks = entity.Remarks,
            Notes = entity.Notes,
            ApprovedBy = entity.ApprovedBy,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt,
            Timeline = BuildVerificationTimeline(entity),
            Attachments = new(),
            Lines = entity.Lines.Select(x => new PhysicalVerificationLineDto
            {
                Id = x.Id.ToString(),
                MaterialId = x.MaterialId,
                MaterialCode = x.MaterialCode,
                MaterialName = x.MaterialName,
                Unit = x.Unit,
                ExpectedQuantity = x.ExpectedQuantity,
                ActualQuantity = x.ActualQuantity,
                Variance = x.Variance,
                Remarks = x.Remarks
            }).ToList()
        };
    }
}
