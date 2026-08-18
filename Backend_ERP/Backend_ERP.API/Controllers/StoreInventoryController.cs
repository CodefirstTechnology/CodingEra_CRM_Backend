using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/store-inventory")]
    [ApiController]
    public class StoreInventoryController : ControllerBase
    {
        private readonly IStoreInventoryService _inventoryService;

        public StoreInventoryController(IStoreInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        // ── Warehouses ──

        [HttpGet("warehouses")]
        public async Task<ActionResult<List<WarehouseListItemDto>>> GetWarehouses(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetWarehousesAsync(new StoreListQueryDto { Search = search, Status = status, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("warehouses/dashboard")]
        public async Task<ActionResult<WarehouseDashboardDto>> GetWarehouseDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetWarehouseDashboardAsync(cancellationToken));
        }

        [HttpGet("warehouses/{id:int}")]
        public async Task<ActionResult<WarehouseDto>> GetWarehouseById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _inventoryService.GetWarehouseByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("warehouses")]
        public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] WarehouseCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _inventoryService.CreateWarehouseAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetWarehouseById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("warehouses/{id:int}")]
        public async Task<ActionResult<WarehouseDto>> UpdateWarehouse(int id, [FromBody] WarehouseCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _inventoryService.UpdateWarehouseAsync(id, request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("warehouses/{id:int}")]
        public async Task<ActionResult> DeleteWarehouse(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var ok = await _inventoryService.DeleteWarehouseAsync(id, ResolveUser(userId), cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        // ── Raw Materials ──

        [HttpGet("raw-materials")]
        [HttpGet("inventory")]
        public async Task<ActionResult<List<RawMaterialListItemDto>>> GetRawMaterials(
            [FromQuery] string? search,
            [FromQuery] int? warehouseId,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetRawMaterialsAsync(new StoreListQueryDto { Search = search, WarehouseId = warehouseId, Category = category, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("raw-materials/dashboard")]
        public async Task<ActionResult<RawMaterialDashboardDto>> GetRawMaterialDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetRawMaterialDashboardAsync(cancellationToken));
        }

        [HttpGet("raw-materials/{id:int}")]
        public async Task<ActionResult<RawMaterialDto>> GetRawMaterialById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _inventoryService.GetRawMaterialByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("raw-materials/adjust")]
        public async Task<ActionResult<RawMaterialDto>> AdjustStock([FromBody] StockAdjustRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _inventoryService.AdjustStockAsync(request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("raw-materials/{id:int}/history")]
        public async Task<ActionResult<List<StockTransactionListItemDto>>> GetRawMaterialHistory(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetRawMaterialHistoryAsync(id, cancellationToken));
        }

        // ── Finished Goods ──

        [HttpGet("finished-goods")]
        public async Task<ActionResult<List<FinishedGoodListItemDto>>> GetFinishedGoods(
            [FromQuery] string? search,
            [FromQuery] int? warehouseId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetFinishedGoodsAsync(new StoreListQueryDto { Search = search, WarehouseId = warehouseId, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("finished-goods/dashboard")]
        public async Task<ActionResult<FinishedGoodDashboardDto>> GetFinishedGoodDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetFinishedGoodDashboardAsync(cancellationToken));
        }

        [HttpGet("finished-goods/{id:int}")]
        public async Task<ActionResult<FinishedGoodDto>> GetFinishedGoodById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _inventoryService.GetFinishedGoodByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("finished-goods/adjust")]
        public async Task<ActionResult<FinishedGoodDto>> AdjustFinishedGood([FromBody] FinishedGoodAdjustRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _inventoryService.AdjustFinishedGoodAsync(request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("finished-goods/{id:int}/history")]
        public async Task<ActionResult<List<StockTransactionListItemDto>>> GetFinishedGoodHistory(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetFinishedGoodHistoryAsync(id, cancellationToken));
        }

        // ── Stock Transactions ──

        [HttpGet("transactions")]
        public async Task<ActionResult<List<StockTransactionListItemDto>>> GetStockTransactions(
            [FromQuery] string? search,
            [FromQuery] string? transactionType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetStockTransactionsAsync(new StoreListQueryDto { Search = search, TransactionType = transactionType, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("transactions/dashboard")]
        public async Task<ActionResult<StockTxnDashboardDto>> GetStockTxnDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetStockTxnDashboardAsync(cancellationToken));
        }

        [HttpGet("transactions/{id:int}")]
        public async Task<ActionResult<StockTransactionDto>> GetStockTransactionById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _inventoryService.GetStockTransactionByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("transactions/stock-in")]
        public async Task<ActionResult<StockTransactionDto>> StockIn([FromBody] StockInRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _inventoryService.StockInAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetStockTransactionById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("transactions/stock-out")]
        public async Task<ActionResult<StockTransactionDto>> StockOut([FromBody] StockOutRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _inventoryService.StockOutAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetStockTransactionById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        // ── Batches ──

        [HttpGet("batches")]
        public async Task<ActionResult<List<InventoryBatchListItemDto>>> GetBatches(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetBatchesAsync(new StoreListQueryDto { Search = search, Status = status, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("batches/dashboard")]
        public async Task<ActionResult<BatchDashboardDto>> GetBatchDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetBatchDashboardAsync(cancellationToken));
        }

        [HttpGet("batches/{id:int}")]
        public async Task<ActionResult<InventoryBatchDto>> GetBatchById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _inventoryService.GetBatchByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        // ── Stock Transfers ──

        [HttpGet("transfers")]
        public async Task<ActionResult<List<StockTransferListItemDto>>> GetTransfers(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetTransfersAsync(new StoreListQueryDto { Search = search, Status = status, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("transfers/dashboard")]
        public async Task<ActionResult<TransferDashboardDto>> GetTransferDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetTransferDashboardAsync(cancellationToken));
        }

        [HttpGet("transfers/{id:int}")]
        public async Task<ActionResult<StockTransferDto>> GetTransferById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _inventoryService.GetTransferByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("transfers")]
        public async Task<ActionResult<StockTransferDto>> Transfer([FromBody] StockTransferCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _inventoryService.TransferAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetTransferById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPatch("transfers/{id:int}/status")]
        public async Task<ActionResult<StockTransferDto>> UpdateTransferStatus(int id, [FromBody] StockTransferStatusUpdateDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _inventoryService.UpdateTransferStatusAsync(id, request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        // ── Stock Alerts ──

        [HttpGet("alerts")]
        public async Task<ActionResult<List<StockAlertDto>>> GetAlerts(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetAlertsAsync(new StoreListQueryDto { Search = search, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("alerts/dashboard")]
        public async Task<ActionResult<AlertDashboardDto>> GetAlertDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetAlertDashboardAsync(cancellationToken));
        }

        // ── Valuation ──

        [HttpGet("valuation")]
        public async Task<ActionResult<StockValuationSummaryDto>> GetValuation([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetValuationAsync(cancellationToken));
        }

        // ── Physical Verification ──

        [HttpGet("verifications")]
        public async Task<ActionResult<List<PhysicalVerificationListItemDto>>> GetVerifications(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetVerificationsAsync(new StoreListQueryDto { Search = search, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("verifications/dashboard")]
        public async Task<ActionResult<VerificationDashboardDto>> GetVerificationDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _inventoryService.GetVerificationDashboardAsync(cancellationToken));
        }

        [HttpGet("verifications/{id:int}")]
        public async Task<ActionResult<PhysicalVerificationDto>> GetVerificationById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _inventoryService.GetVerificationByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("verifications")]
        public async Task<ActionResult<PhysicalVerificationDto>> VerifyStock([FromBody] PhysicalVerificationCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _inventoryService.VerifyStockAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetVerificationById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPatch("verifications/{id:int}/status")]
        public async Task<ActionResult<PhysicalVerificationDto>> UpdateVerificationStatus(int id, [FromBody] PhysicalVerificationStatusUpdateDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _inventoryService.UpdateVerificationStatusAsync(id, request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(await _inventoryService.GetPermissionsAsync());
        }

        private static string ResolveUser(int? userId) => userId is > 0 ? userId.Value.ToString() : "system";
    }
}
