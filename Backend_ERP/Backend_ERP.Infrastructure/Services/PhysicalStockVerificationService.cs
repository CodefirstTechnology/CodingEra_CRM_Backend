using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Application.DTOs.Inventory;
using Backend_ERP.Domain.Entities;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend_ERP.Infrastructure.Services
{
    public interface IPhysicalStockVerificationService
    {
        Task<string> CreateCountSheetAsync(CreateCountSheetDto dto, CancellationToken ct = default);
        Task SubmitCountsAsync(Guid sheetId, List<CountLineCreateDto> counts, CancellationToken ct = default);
        Task<string> ReconcileCountSheetAsync(Guid sheetId, ReconcileCountSheetDto dto, CancellationToken ct = default);
        Task<CountSheetDto?> GetCountSheetAsync(Guid sheetId, CancellationToken ct = default);
    }

    public class PhysicalStockVerificationService : IPhysicalStockVerificationService
    {
        private readonly ERPDbContext _dbContext;
        private readonly IHardenedStockTransactionService _stockTransactionService;
        private readonly IStockValuationService _valuationService;
        private readonly ILogger<PhysicalStockVerificationService> _logger;

        public PhysicalStockVerificationService(
            ERPDbContext dbContext,
            IHardenedStockTransactionService stockTransactionService,
            IStockValuationService valuationService,
            ILogger<PhysicalStockVerificationService> logger)
        {
            _dbContext = dbContext;
            _stockTransactionService = stockTransactionService;
            _valuationService = valuationService;
            _logger = logger;
        }

        public async Task<string> CreateCountSheetAsync(CreateCountSheetDto dto, CancellationToken ct = default)
        {
            var sheetNo = $"PCS-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
            var sheet = new PhysicalCountSheetEntity
            {
                SheetNumber = sheetNo,
                WarehouseId = dto.WarehouseId,
                CountDate = DateTime.UtcNow,
                Status = "Draft",
                ConductedByUserId = dto.ConductedByUserId,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var line in dto.Lines)
            {
                decimal variance = line.CountedQty - line.SystemQty;
                sheet.Lines.Add(new PhysicalCountLineEntity
                {
                    ItemId = line.ItemId,
                    BinId = line.BinId,
                    BatchId = line.BatchId,
                    SystemQty = line.SystemQty,
                    CountedQty = line.CountedQty,
                    VarianceQty = variance,
                    VarianceReason = line.VarianceReason
                });
            }

            _dbContext.PhysicalCountSheets.Add(sheet);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Created Physical Count Sheet {SheetNo} for Warehouse {WarehouseId}.", sheetNo, dto.WarehouseId);
            return sheetNo;
        }

        public async Task SubmitCountsAsync(Guid sheetId, List<CountLineCreateDto> counts, CancellationToken ct = default)
        {
            var sheet = await _dbContext.PhysicalCountSheets
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.SheetId == sheetId, ct)
                ?? throw new KeyNotFoundException($"Physical count sheet {sheetId} not found.");

            if (sheet.Status != "Draft")
                throw new InvalidOperationException($"Count sheet {sheet.SheetNumber} cannot be modified in status '{sheet.Status}'.");

            foreach (var count in counts)
            {
                var line = sheet.Lines.FirstOrDefault(l => l.ItemId == count.ItemId && l.BinId == count.BinId && l.BatchId == count.BatchId);
                if (line != null)
                {
                    line.CountedQty = count.CountedQty;
                    line.VarianceQty = count.CountedQty - line.SystemQty;
                    line.VarianceReason = count.VarianceReason;
                }
            }

            sheet.Status = "Submitted";
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<string> ReconcileCountSheetAsync(Guid sheetId, ReconcileCountSheetDto dto, CancellationToken ct = default)
        {
            await using var dbTxn = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var sheet = await _dbContext.PhysicalCountSheets
                    .Include(s => s.Lines)
                    .FirstOrDefaultAsync(s => s.SheetId == sheetId, ct)
                    ?? throw new KeyNotFoundException($"Physical count sheet {sheetId} not found.");

                if (sheet.Status == "Reconciled")
                    throw new InvalidOperationException($"Count sheet {sheet.SheetNumber} is already reconciled.");

                // Deterministically sort count lines by (ItemId, BinId, BatchId) to prevent deadlocks
                var sortedLines = sheet.Lines
                    .OrderBy(l => l.ItemId)
                    .ThenBy(l => l.BinId ?? Guid.Empty)
                    .ThenBy(l => l.BatchId ?? Guid.Empty)
                    .ToList();

                foreach (var line in sortedLines)
                {
                    if (line.VarianceQty == 0) continue;

                    Guid idempotencyKey = Guid.NewGuid();

                    if (line.VarianceQty > 0)
                    {
                        // Positive Variance (Stock Gain) -> ADJ_POS_IN
                        var movementRequest = new BatchStockMovementRequestDto(
                            idempotencyKey,
                            TransactionType.ADJ_POS_IN,
                            "PhysicalCountSheet",
                            sheet.SheetId,
                            sheet.SheetNumber,
                            Guid.Empty,
                            new List<StockMovementLineDto>
                            {
                                new StockMovementLineDto(
                                    Guid.Empty,
                                    sheet.WarehouseId,
                                    line.BinId ?? Guid.Empty,
                                    line.BatchId?.ToString(),
                                    line.VarianceQty,
                                    0m,
                                    0m
                                )
                            }
                        );

                        await _stockTransactionService.ExecuteBatchStockMovementAsync(movementRequest, ct);

                        // Record Inward Valuation Layer
                        await _valuationService.RecordInwardReceiptAsync(new ValuationInwardReceiptDto
                        {
                            ItemId = line.ItemId,
                            WarehouseId = sheet.WarehouseId,
                            Quantity = line.VarianceQty,
                            UnitCost = 0m,
                            ReceiptDate = DateTime.UtcNow
                        }, ct);
                    }
                    else if (line.VarianceQty < 0)
                    {
                        // Negative Variance (Stock Loss) -> ADJ_NEG_OUT
                        decimal absLossQty = Math.Abs(line.VarianceQty);

                        var movementRequest = new BatchStockMovementRequestDto(
                            idempotencyKey,
                            TransactionType.ADJ_NEG_OUT,
                            "PhysicalCountSheet",
                            sheet.SheetId,
                            sheet.SheetNumber,
                            Guid.Empty,
                            new List<StockMovementLineDto>
                            {
                                new StockMovementLineDto(
                                    Guid.Empty,
                                    sheet.WarehouseId,
                                    line.BinId ?? Guid.Empty,
                                    line.BatchId?.ToString(),
                                    absLossQty,
                                    0m,
                                    0m
                                )
                            }
                        );

                        await _stockTransactionService.ExecuteBatchStockMovementAsync(movementRequest, ct);

                        // Deplete FIFO Valuation Layers
                        await _valuationService.ConsumeValuationOnStockOutAsync(new ValuationOutwardDepletionDto
                        {
                            ItemId = line.ItemId,
                            WarehouseId = sheet.WarehouseId,
                            Quantity = absLossQty
                        }, ct);
                    }
                }

                sheet.Status = "Reconciled";
                sheet.ReconciledByUserId = dto.ReconciledByUserId;
                sheet.ReconciledAt = DateTime.UtcNow;

                // Outbox Event
                var eventPayload = JsonSerializer.Serialize(new
                {
                    sheet.SheetId,
                    sheet.SheetNumber,
                    sheet.WarehouseId,
                    ReconciledBy = dto.ReconciledByUserId,
                    ReconciledAt = sheet.ReconciledAt
                });

                _dbContext.OutboxMessages.Add(new OutboxMessageEntity
                {
                    EventType = "PhysicalSheetReconciledEvent",
                    PayloadJson = eventPayload
                });

                await _dbContext.SaveChangesAsync(ct);
                await dbTxn.CommitAsync(ct);

                _logger.LogInformation("Physical Count Sheet {SheetNo} reconciled successfully.", sheet.SheetNumber);
                return sheet.SheetNumber;
            }
            catch (Exception ex)
            {
                await dbTxn.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to reconcile count sheet {SheetId}.", sheetId);
                throw;
            }
        }

        public async Task<CountSheetDto?> GetCountSheetAsync(Guid sheetId, CancellationToken ct = default)
        {
            var sheet = await _dbContext.PhysicalCountSheets
                .Include(s => s.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SheetId == sheetId, ct);

            if (sheet == null) return null;

            return new CountSheetDto
            {
                SheetId = sheet.SheetId,
                SheetNumber = sheet.SheetNumber,
                WarehouseId = sheet.WarehouseId,
                CountDate = sheet.CountDate,
                Status = sheet.Status,
                ConductedByUserId = sheet.ConductedByUserId,
                ReconciledByUserId = sheet.ReconciledByUserId,
                ReconciledAt = sheet.ReconciledAt,
                Lines = sheet.Lines.Select(l => new CountLineDto
                {
                    LineId = l.LineId,
                    ItemId = l.ItemId,
                    BinId = l.BinId,
                    BatchId = l.BatchId,
                    SystemQty = l.SystemQty,
                    CountedQty = l.CountedQty,
                    VarianceQty = l.VarianceQty,
                    VarianceReason = l.VarianceReason
                }).ToList()
            };
        }
    }
}
