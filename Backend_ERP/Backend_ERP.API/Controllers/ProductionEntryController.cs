using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Infrastructure.Production;
using ERP.Application.Production.Dtos;
using ERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/production/shop-floor-entries")]
    public class ProductionEntryController : ControllerBase
    {
        private readonly IAtomicProductionEntryHandler _entryHandler;
        private readonly ERPDbContext _dbContext;

        public ProductionEntryController(IAtomicProductionEntryHandler entryHandler, ERPDbContext dbContext)
        {
            _entryHandler = entryHandler ?? throw new ArgumentNullException(nameof(entryHandler));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        [HttpPost]
        public async Task<ActionResult<ProductionEntryResponseDto>> CreateProductionEntry(
            [FromBody] CreateProductionEntryRequestDto request,
            [FromQuery] string? actingUser,
            CancellationToken ct = default)
        {
            try
            {
                var user = !string.IsNullOrWhiteSpace(actingUser) ? actingUser : "operator-terminal";
                var result = await _entryHandler.ProcessProductionEntryAsync(request, user, ct);
                return CreatedAtAction(nameof(GetEntryById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductionEntryResponseDto>> GetEntryById(Guid id, CancellationToken ct = default)
        {
            var entry = await _dbContext.ShopFloorProductionEntries
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            if (entry == null) return NotFound();

            var consumptions = await _dbContext.MaterialConsumptionLogs
                .Where(c => c.ProductionEntryId == id)
                .Select(c => new MaterialConsumptionDto
                {
                    RawMaterialItemId = c.RawMaterialItemId,
                    BatchId = c.BatchId,
                    StandardBomQuantity = c.StandardBomQuantity,
                    ActualConsumedQuantity = c.ActualConsumedQuantity,
                    ConsumptionType = c.ConsumptionType
                })
                .ToListAsync(ct);

            var rejections = await _dbContext.RejectionTrackingLogs
                .Where(r => r.ProductionEntryId == id)
                .Select(r => new RejectionLogDto
                {
                    DefectCategory = r.DefectCategory,
                    DefectReasonCode = r.DefectReasonCode,
                    RejectedQuantity = r.RejectedQuantity,
                    Disposition = r.Disposition,
                    UnitScrapCost = r.UnitScrapCost
                })
                .ToListAsync(ct);

            return Ok(new ProductionEntryResponseDto
            {
                Id = entry.Id,
                EntryNumber = entry.EntryNumber,
                WorkOrderOperationId = entry.WorkOrderOperationId,
                ShiftId = entry.ShiftId,
                OperatorId = entry.OperatorId,
                MachineId = entry.MachineId,
                GoodQuantity = entry.GoodQuantity,
                ScrappedQuantity = entry.ScrappedQuantity,
                StartTime = entry.StartTime,
                EndTime = entry.EndTime,
                Status = entry.Status,
                SupervisorApprovedBy = entry.SupervisorApprovedBy,
                SupervisorApprovedAt = entry.SupervisorApprovedAt,
                CreatedAt = entry.CreatedAt,
                Consumptions = consumptions,
                Rejections = rejections
            });
        }

        [HttpGet("operation/{operationId:guid}")]
        public async Task<ActionResult<List<ProductionEntryResponseDto>>> GetEntriesByOperation(Guid operationId, CancellationToken ct = default)
        {
            var entries = await _dbContext.ShopFloorProductionEntries
                .Where(e => e.WorkOrderOperationId == operationId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(ct);

            var result = new List<ProductionEntryResponseDto>();
            foreach (var entry in entries)
            {
                var consumptions = await _dbContext.MaterialConsumptionLogs
                    .Where(c => c.ProductionEntryId == entry.Id)
                    .Select(c => new MaterialConsumptionDto
                    {
                        RawMaterialItemId = c.RawMaterialItemId,
                        BatchId = c.BatchId,
                        StandardBomQuantity = c.StandardBomQuantity,
                        ActualConsumedQuantity = c.ActualConsumedQuantity,
                        ConsumptionType = c.ConsumptionType
                    })
                    .ToListAsync(ct);

                var rejections = await _dbContext.RejectionTrackingLogs
                    .Where(r => r.ProductionEntryId == entry.Id)
                    .Select(r => new RejectionLogDto
                    {
                        DefectCategory = r.DefectCategory,
                        DefectReasonCode = r.DefectReasonCode,
                        RejectedQuantity = r.RejectedQuantity,
                        Disposition = r.Disposition,
                        UnitScrapCost = r.UnitScrapCost
                    })
                    .ToListAsync(ct);

                result.Add(new ProductionEntryResponseDto
                {
                    Id = entry.Id,
                    EntryNumber = entry.EntryNumber,
                    WorkOrderOperationId = entry.WorkOrderOperationId,
                    ShiftId = entry.ShiftId,
                    OperatorId = entry.OperatorId,
                    MachineId = entry.MachineId,
                    GoodQuantity = entry.GoodQuantity,
                    ScrappedQuantity = entry.ScrappedQuantity,
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    Status = entry.Status,
                    SupervisorApprovedBy = entry.SupervisorApprovedBy,
                    SupervisorApprovedAt = entry.SupervisorApprovedAt,
                    CreatedAt = entry.CreatedAt,
                    Consumptions = consumptions,
                    Rejections = rejections
                });
            }

            return Ok(result);
        }
    }
}
