using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Application.DTOs.Inventory;
using Backend_ERP.Domain.Entities;
using ERP.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_ERP.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-alerts")]
    [Produces("application/json")]
    public class StockAlertController : ControllerBase
    {
        private readonly ERPDbContext _dbContext;

        public StockAlertController(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Configures minimum stock threshold rules for an item and warehouse
        /// </summary>
        [HttpPost("thresholds")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetThreshold([FromBody] SetThresholdDto request, CancellationToken ct)
        {
            var existing = await _dbContext.StockAlertThresholds
                .FirstOrDefaultAsync(t => t.ItemId == request.ItemId && t.WarehouseId == request.WarehouseId, ct);

            if (existing != null)
            {
                existing.MinStockLevel = request.MinStockLevel;
                existing.ReorderPoint = request.ReorderPoint;
                existing.MaxStockLevel = request.MaxStockLevel;
                existing.SafetyStock = request.SafetyStock;
                existing.IsActive = true;
            }
            else
            {
                _dbContext.StockAlertThresholds.Add(new StockAlertThresholdEntity
                {
                    ItemId = request.ItemId,
                    WarehouseId = request.WarehouseId,
                    MinStockLevel = request.MinStockLevel,
                    ReorderPoint = request.ReorderPoint,
                    MaxStockLevel = request.MaxStockLevel,
                    SafetyStock = request.SafetyStock,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync(ct);
            return Ok(new { status = "Success", message = "Stock alert threshold configured successfully." });
        }

        /// <summary>
        /// Retrieves active stock alert logs across warehouses
        /// </summary>
        [HttpGet("logs")]
        [ProducesResponseType(typeof(List<StockAlertDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlertLogs([FromQuery] Guid? warehouseId, [FromQuery] string? status = "Active", CancellationToken ct = default)
        {
            var query = _dbContext.StockAlertLogs.AsNoTracking();

            if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
                query = query.Where(a => a.WarehouseId == warehouseId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new StockAlertDto
                {
                    AlertId = a.AlertId,
                    ThresholdId = a.ThresholdId,
                    ItemId = a.ItemId,
                    WarehouseId = a.WarehouseId,
                    AlertType = a.AlertType,
                    AlertPriority = a.AlertPriority,
                    CurrentQty = a.CurrentQty,
                    AvailableQty = a.AvailableQty,
                    OnOrderQty = a.OnOrderQty,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(logs);
        }
    }
}
