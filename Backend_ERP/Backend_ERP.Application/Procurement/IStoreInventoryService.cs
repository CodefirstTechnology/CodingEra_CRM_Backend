using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IStoreInventoryService
    {
        // Warehouses
        Task<List<WarehouseListItemDto>> GetWarehousesAsync(StoreListQueryDto query, CancellationToken cancellationToken = default);
        Task<WarehouseDto?> GetWarehouseByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<WarehouseDto> CreateWarehouseAsync(WarehouseCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<WarehouseDto?> UpdateWarehouseAsync(int id, WarehouseCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<bool> DeleteWarehouseAsync(int id, string currentUser, CancellationToken cancellationToken = default);
        Task<WarehouseDashboardDto> GetWarehouseDashboardAsync(CancellationToken cancellationToken = default);

        // Raw Materials
        Task<List<RawMaterialListItemDto>> GetRawMaterialsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default);
        Task<RawMaterialDto?> GetRawMaterialByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<RawMaterialDto> CreateRawMaterialAsync(RawMaterialCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<RawMaterialDto?> AdjustStockAsync(StockAdjustRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<List<StockTransactionListItemDto>> GetRawMaterialHistoryAsync(int id, CancellationToken cancellationToken = default);
        Task<RawMaterialDashboardDto> GetRawMaterialDashboardAsync(CancellationToken cancellationToken = default);

        // Finished Goods
        Task<List<FinishedGoodListItemDto>> GetFinishedGoodsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default);
        Task<FinishedGoodDto?> GetFinishedGoodByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<FinishedGoodDto> CreateFinishedGoodAsync(FinishedGoodCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<FinishedGoodDto?> AdjustFinishedGoodAsync(FinishedGoodAdjustRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<List<StockTransactionListItemDto>> GetFinishedGoodHistoryAsync(int id, CancellationToken cancellationToken = default);
        Task<FinishedGoodDashboardDto> GetFinishedGoodDashboardAsync(CancellationToken cancellationToken = default);

        // Stock Transactions
        Task<List<StockTransactionListItemDto>> GetStockTransactionsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default);
        Task<StockTransactionDto?> GetStockTransactionByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<StockTransactionDto> StockInAsync(StockInRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<StockTransactionDto> StockOutAsync(StockOutRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<StockTxnDashboardDto> GetStockTxnDashboardAsync(CancellationToken cancellationToken = default);

        // Batches
        Task<List<InventoryBatchListItemDto>> GetBatchesAsync(StoreListQueryDto query, CancellationToken cancellationToken = default);
        Task<InventoryBatchDto?> GetBatchByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<InventoryBatchDto> CreateBatchAsync(InventoryBatchCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<BatchDashboardDto> GetBatchDashboardAsync(CancellationToken cancellationToken = default);

        // Transfers
        Task<List<StockTransferListItemDto>> GetTransfersAsync(StoreListQueryDto query, CancellationToken cancellationToken = default);
        Task<StockTransferDto?> GetTransferByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<StockTransferDto> TransferAsync(StockTransferCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<StockTransferDto?> UpdateTransferStatusAsync(int id, StockTransferStatusUpdateDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<TransferDashboardDto> GetTransferDashboardAsync(CancellationToken cancellationToken = default);

        // Alerts
        Task<List<StockAlertDto>> GetAlertsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default);
        Task<AlertDashboardDto> GetAlertDashboardAsync(CancellationToken cancellationToken = default);

        // Valuation
        Task<StockValuationSummaryDto> GetValuationAsync(CancellationToken cancellationToken = default);

        // Physical Verification
        Task<List<PhysicalVerificationListItemDto>> GetVerificationsAsync(StoreListQueryDto query, CancellationToken cancellationToken = default);
        Task<PhysicalVerificationDto?> GetVerificationByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PhysicalVerificationDto> VerifyStockAsync(PhysicalVerificationCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<PhysicalVerificationDto?> UpdateVerificationStatusAsync(int id, PhysicalVerificationStatusUpdateDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<VerificationDashboardDto> GetVerificationDashboardAsync(CancellationToken cancellationToken = default);
        Task<List<ErpItemLookupDto>> LookupItemsAsync(string? search, int pageSize = 100, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetPermissionsAsync();
    }
}
