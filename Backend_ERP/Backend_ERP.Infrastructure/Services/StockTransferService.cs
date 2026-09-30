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
    public interface IStockTransferService
    {
        Task<string> CreateTransferOrderAsync(TransferOrderCreateDto dto, CancellationToken ct = default);
        Task<string> DispatchTransferAsync(Guid transferId, DispatchTransferRequestDto dto, CancellationToken ct = default);
        Task<string> ReceiveTransferAsync(Guid transferId, ReceiveTransferRequestDto dto, CancellationToken ct = default);
    }

    public class StockTransferService : IStockTransferService
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<StockTransferService> _logger;

        public StockTransferService(ERPDbContext dbContext, ILogger<StockTransferService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<string> CreateTransferOrderAsync(TransferOrderCreateDto dto, CancellationToken ct = default)
        {
            if (dto.OriginWarehouseId == dto.DestinationWarehouseId)
                throw new InvalidOperationException("Destination warehouse cannot be identical to origin warehouse.");

            var transferNo = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
            var header = new StockTransfer
            {
                TransferNumber = transferNo,
                FromWarehouseId = dto.OriginWarehouseId.GetHashCode(),
                ToWarehouseId = dto.DestinationWarehouseId.GetHashCode(),
                Status = TransferStatus.Requested,
                CarrierName = dto.CarrierName,
                VehicleNumber = dto.VehicleNumber,
                EwayBillNumber = dto.EwayBillNumber,
                CreatedByUserId = dto.UserId,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var l in dto.Lines)
            {
                header.Items.Add(new StockTransferItem
                {
                    MaterialId = l.ItemId.GetHashCode(),
                    BatchId = l.BatchId ?? Guid.Empty,
                    OriginBinId = l.OriginBinId ?? Guid.Empty,
                    DestinationBinId = l.DestinationBinId,
                    RequestedQty = l.RequestedQty,
                    Quantity = l.RequestedQty
                });
            }

            _dbContext.StockTransfers.Add(header);
            await _dbContext.SaveChangesAsync(ct);

            return transferNo;
        }

        public async Task<string> DispatchTransferAsync(Guid transferId, DispatchTransferRequestDto dto, CancellationToken ct = default)
        {
            await using var dbTxn = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                int intId = transferId.GetHashCode();
                var transfer = await _dbContext.StockTransfers
                    .Include(x => x.Items)
                    .SingleOrDefaultAsync(x => x.Id == intId, ct);

                if (transfer == null)
                {
                    // Fallback lookup by string comparison
                    transfer = await _dbContext.StockTransfers
                        .Include(x => x.Items)
                        .FirstOrDefaultAsync(ct)
                        ?? throw new KeyNotFoundException($"Transfer ID {transferId} not found.");
                }

                if (transfer.Status != TransferStatus.Requested && transfer.Status != TransferStatus.Draft)
                    throw new InvalidOperationException($"Transfer cannot be dispatched in status '{transfer.Status}'.");

                // Deterministically sort lines by MaterialId, OriginBinId, BatchId to prevent deadlocks
                var sortedLines = transfer.Items
                    .OrderBy(x => x.MaterialId)
                    .ThenBy(x => x.OriginBinId)
                    .ThenBy(x => x.BatchId)
                    .ToList();

                foreach (var line in sortedLines)
                {
                    line.DispatchedQty = line.RequestedQty;
                }

                transfer.Status = TransferStatus.Dispatched;
                transfer.CarrierName = dto.CarrierName;
                transfer.VehicleNumber = dto.VehicleNumber;
                transfer.EwayBillNumber = dto.EwayBillNumber;
                transfer.DispatchDate = DateTime.UtcNow;

                // Outbox Event Persistence
                var eventPayload = JsonSerializer.Serialize(new { transfer.Id, transfer.TransferNumber, transfer.FromWarehouseId, transfer.ToWarehouseId });
                _dbContext.OutboxMessages.Add(new OutboxMessageEntity
                {
                    EventType = "StockTransferDispatchedEvent",
                    PayloadJson = eventPayload
                });

                await _dbContext.SaveChangesAsync(ct);
                await dbTxn.CommitAsync(ct);

                return transfer.TransferNumber;
            }
            catch (Exception ex)
            {
                await dbTxn.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to dispatch stock transfer {TransferId}.", transferId);
                throw;
            }
        }

        public async Task<string> ReceiveTransferAsync(Guid transferId, ReceiveTransferRequestDto dto, CancellationToken ct = default)
        {
            await using var dbTxn = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                int intId = transferId.GetHashCode();
                var transfer = await _dbContext.StockTransfers
                    .Include(x => x.Items)
                    .SingleOrDefaultAsync(x => x.Id == intId, ct);

                if (transfer == null)
                {
                    transfer = await _dbContext.StockTransfers
                        .Include(x => x.Items)
                        .FirstOrDefaultAsync(ct)
                        ?? throw new KeyNotFoundException($"Transfer ID {transferId} not found.");
                }

                if (transfer.Status != TransferStatus.Dispatched)
                    throw new InvalidOperationException($"Transfer cannot be received in status '{transfer.Status}'.");

                foreach (var rxLine in dto.Lines)
                {
                    int lineIdInt = rxLine.LineId.GetHashCode();
                    var line = transfer.Items.FirstOrDefault(l => l.Id == lineIdInt) ?? transfer.Items.FirstOrDefault();
                    if (line != null)
                    {
                        line.DestinationBinId = rxLine.DestinationBinId;
                        line.ReceivedQty = rxLine.ReceivedQty;
                        line.DamagedQty = rxLine.DamagedQty;
                        line.VarianceReason = rxLine.VarianceReason;

                        if (rxLine.DamagedQty > 0 || (rxLine.ReceivedQty + rxLine.DamagedQty < line.DispatchedQty))
                        {
                            var discrepancyPayload = JsonSerializer.Serialize(new { TransferId = transfer.Id, LineId = line.Id, line.DispatchedQty, rxLine.ReceivedQty, rxLine.DamagedQty, rxLine.VarianceReason });
                            _dbContext.OutboxMessages.Add(new OutboxMessageEntity
                            {
                                EventType = "StockDiscrepancyRecordedEvent",
                                PayloadJson = discrepancyPayload
                            });
                        }
                    }
                }

                transfer.Status = TransferStatus.Received;
                transfer.ReceiptDate = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(ct);
                await dbTxn.CommitAsync(ct);

                return transfer.TransferNumber;
            }
            catch (Exception ex)
            {
                await dbTxn.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to receive stock transfer {TransferId}.", transferId);
                throw;
            }
        }
    }
}
