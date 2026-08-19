using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class QualityControlService : IQualityControlService
    {
        private readonly ERPDbContext _db;
        private readonly QualityControlNumberingService _numberingService;

        public QualityControlService(ERPDbContext db, QualityControlNumberingService numberingService)
        {
            _db = db;
            _numberingService = numberingService;
        }

        // ── Incoming Inspection ──

        public async Task<PagedResult<IncomingListItemDto>> GetIncomingInspectionsAsync(ListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _db.IncomingInspections.AsNoTracking().Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.InspectionNumber.ToLower().Contains(term) ||
                                 x.VendorName.ToLower().Contains(term) ||
                                 x.PurchaseOrderNumber.ToLower().Contains(term) ||
                                 x.GRNNumber.ToLower().Contains(term) ||
                                 x.MaterialName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<IncomingInspectionStatus>(query.Status, true, out var st))
            {
                q = q.Where(x => x.Status == st);
            }

            var total = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 1000);

            var list = await q.OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken);

            var dtos = list.Select(MapToIncomingListItem).ToList();
            return PagedResult<IncomingListItemDto>.Create(dtos, total, page, size);
        }

        public async Task<IncomingDto?> GetIncomingByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.IncomingInspections
                .Include(x => x.Checklist)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return entity is null ? null : MapToIncomingDto(entity);
        }

        public async Task<IncomingDto> CreateIncomingAsync(IncomingCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var valErr = QualityControlRules.ValidateIncomingQuantities(request.SamplingQuantity, request.AcceptedQuantity, request.RejectedQuantity, request.PendingQuantity);
            if (valErr != null) throw new InvalidOperationException(valErr);

            var now = DateTime.UtcNow;
            var num = await _numberingService.NextNumberAsync("INSP", cancellationToken);

            var entity = new IncomingInspection
            {
                InspectionNumber = num,
                InspectionDate = request.InspectionDate.ToUniversalTime(),
                SupplierId = request.SupplierId,
                SupplierName = request.SupplierName.Trim(),
                VendorId = request.VendorId,
                VendorName = request.VendorName.Trim(),
                PurchaseOrderId = request.PurchaseOrderId,
                PurchaseOrderNumber = request.PurchaseOrderNumber.Trim(),
                GRNId = request.GRNId,
                GRNNumber = request.GRNNumber.Trim(),
                MaterialId = request.MaterialId,
                MaterialCode = request.MaterialCode.Trim(),
                MaterialName = request.MaterialName.Trim(),
                BatchNumber = request.BatchNumber.Trim(),
                WarehouseId = request.WarehouseId,
                WarehouseName = request.WarehouseName.Trim(),
                Inspector = request.Inspector.Trim(),
                InspectionType = request.InspectionType,
                InspectionMethod = request.InspectionMethod,
                SamplingQuantity = request.SamplingQuantity,
                AcceptedQuantity = request.AcceptedQuantity,
                RejectedQuantity = request.RejectedQuantity,
                PendingQuantity = request.PendingQuantity,
                InspectionResult = ResolveIncomingResult(request.AcceptedQuantity, request.RejectedQuantity, request.SamplingQuantity),
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                Status = IncomingInspectionStatus.Draft,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            foreach (var chk in request.Checklist ?? new())
            {
                entity.Checklist.Add(new IncomingChecklistItem
                {
                    Parameter = chk.Parameter.Trim(),
                    Specification = chk.Specification.Trim(),
                    ActualValue = chk.ActualValue.Trim(),
                    Result = chk.Result,
                    Remarks = chk.Remarks?.Trim() ?? string.Empty
                });
            }

            _db.IncomingInspections.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetIncomingByIdAsync(entity.Id, cancellationToken))!;
        }

        public async Task<IncomingDto?> UpdateIncomingAsync(int id, IncomingCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.IncomingInspections.Include(x => x.Checklist).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (entity.Status != IncomingInspectionStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft inspections can be edited.");
            }

            var valErr = QualityControlRules.ValidateIncomingQuantities(request.SamplingQuantity, request.AcceptedQuantity, request.RejectedQuantity, request.PendingQuantity);
            if (valErr != null) throw new InvalidOperationException(valErr);

            var now = DateTime.UtcNow;

            entity.InspectionDate = request.InspectionDate.ToUniversalTime();
            entity.SupplierId = request.SupplierId;
            entity.SupplierName = request.SupplierName.Trim();
            entity.VendorId = request.VendorId;
            entity.VendorName = request.VendorName.Trim();
            entity.PurchaseOrderId = request.PurchaseOrderId;
            entity.PurchaseOrderNumber = request.PurchaseOrderNumber.Trim();
            entity.GRNId = request.GRNId;
            entity.GRNNumber = request.GRNNumber.Trim();
            entity.MaterialId = request.MaterialId;
            entity.MaterialCode = request.MaterialCode.Trim();
            entity.MaterialName = request.MaterialName.Trim();
            entity.BatchNumber = request.BatchNumber.Trim();
            entity.WarehouseId = request.WarehouseId;
            entity.WarehouseName = request.WarehouseName.Trim();
            entity.Inspector = request.Inspector.Trim();
            entity.InspectionType = request.InspectionType;
            entity.InspectionMethod = request.InspectionMethod;
            entity.SamplingQuantity = request.SamplingQuantity;
            entity.AcceptedQuantity = request.AcceptedQuantity;
            entity.RejectedQuantity = request.RejectedQuantity;
            entity.PendingQuantity = request.PendingQuantity;
            entity.InspectionResult = ResolveIncomingResult(request.AcceptedQuantity, request.RejectedQuantity, request.SamplingQuantity);
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.UpdatedAt = now;
            entity.UpdatedBy = actingUser;

            _db.IncomingChecklistItems.RemoveRange(entity.Checklist);
            entity.Checklist.Clear();

            foreach (var chk in request.Checklist ?? new())
            {
                entity.Checklist.Add(new IncomingChecklistItem
                {
                    IncomingInspectionId = id,
                    Parameter = chk.Parameter.Trim(),
                    Specification = chk.Specification.Trim(),
                    ActualValue = chk.ActualValue.Trim(),
                    Result = chk.Result,
                    Remarks = chk.Remarks?.Trim() ?? string.Empty
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetIncomingByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteIncomingAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.IncomingInspections.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            if (entity.Status != IncomingInspectionStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft inspections can be deleted.");
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IncomingDto?> DuplicateIncomingAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var src = await _db.IncomingInspections.Include(x => x.Checklist).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (src is null) return null;

            var req = new IncomingCreateRequestDto
            {
                InspectionDate = DateTime.UtcNow,
                SupplierId = src.SupplierId,
                SupplierName = src.SupplierName,
                VendorId = src.VendorId,
                VendorName = src.VendorName,
                PurchaseOrderId = src.PurchaseOrderId,
                PurchaseOrderNumber = src.PurchaseOrderNumber,
                GRNId = src.GRNId,
                GRNNumber = src.GRNNumber,
                MaterialId = src.MaterialId,
                MaterialCode = src.MaterialCode,
                MaterialName = src.MaterialName,
                BatchNumber = src.BatchNumber,
                WarehouseId = src.WarehouseId,
                WarehouseName = src.WarehouseName,
                Inspector = src.Inspector,
                InspectionType = src.InspectionType,
                InspectionMethod = src.InspectionMethod,
                SamplingQuantity = src.SamplingQuantity,
                AcceptedQuantity = src.AcceptedQuantity,
                RejectedQuantity = src.RejectedQuantity,
                PendingQuantity = src.PendingQuantity,
                Remarks = $"Duplicate of {src.InspectionNumber}",
                Notes = src.Notes,
                Checklist = src.Checklist.Select(c => new QcChecklistItemDto
                {
                    Parameter = c.Parameter,
                    Specification = c.Specification,
                    ActualValue = c.ActualValue,
                    Result = c.Result,
                    Remarks = c.Remarks
                }).ToList()
            };

            return await CreateIncomingAsync(req, actingUser, cancellationToken);
        }

        public async Task<IncomingDto?> SubmitIncomingAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeIncomingStatusAsync(id, IncomingInspectionStatus.Submitted, payload?.Remarks, actingUser, cancellationToken);

        public async Task<IncomingDto?> ApproveIncomingAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeIncomingStatusAsync(id, IncomingInspectionStatus.Approved, payload?.Remarks, actingUser, cancellationToken);

        public async Task<IncomingDto?> RejectIncomingAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeIncomingStatusAsync(id, IncomingInspectionStatus.Rejected, payload?.Remarks, actingUser, cancellationToken);

        public async Task<IncomingDto?> CloseIncomingAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeIncomingStatusAsync(id, IncomingInspectionStatus.Closed, payload?.Remarks, actingUser, cancellationToken);

        public async Task<IncomingDto?> RecordInspectionAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeIncomingStatusAsync(id, IncomingInspectionStatus.Submitted, payload?.Remarks ?? "Inspection recorded.", actingUser, cancellationToken);

        public async Task<IncomingDashboardDto> GetIncomingDashboardAsync(CancellationToken cancellationToken = default)
        {
            var q = _db.IncomingInspections.AsNoTracking().Where(x => !x.IsDeleted);
            var today = DateTime.UtcNow.Date;

            return new IncomingDashboardDto
            {
                PendingInspection = await q.CountAsync(x => x.Status == IncomingInspectionStatus.Draft || x.Status == IncomingInspectionStatus.Submitted, cancellationToken),
                Approved = await q.CountAsync(x => x.Status == IncomingInspectionStatus.Approved, cancellationToken),
                Rejected = await q.CountAsync(x => x.Status == IncomingInspectionStatus.Rejected, cancellationToken),
                TodaysInspection = await q.CountAsync(x => x.InspectionDate >= today, cancellationToken)
            };
        }

        private async Task<IncomingDto?> ChangeIncomingStatusAsync(int id, IncomingInspectionStatus target, string? remarks, string actingUser, CancellationToken cancellationToken)
        {
            var entity = await _db.IncomingInspections.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (!QualityControlRules.CanTransitionIncoming(entity.Status, target))
            {
                throw new InvalidOperationException($"Cannot transition incoming inspection from {entity.Status} to {target}.");
            }

            entity.Status = target;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            if (!string.IsNullOrWhiteSpace(remarks)) entity.Remarks = remarks.Trim();

            await _db.SaveChangesAsync(cancellationToken);

            if (target == IncomingInspectionStatus.Rejected)
            {
                var existingRejection = await _db.RejectionAnalyses.FirstOrDefaultAsync(
                    x => x.Source == RejectionSource.Incoming && x.SourceRecordId == entity.Id && !x.IsDeleted,
                    cancellationToken);

                if (existingRejection is null)
                {
                    var rejRequest = new RejectionCreateRequestDto
                    {
                        Source = RejectionSource.Incoming,
                        SourceRecordId = entity.Id,
                        SourceRecordNumber = entity.InspectionNumber,
                        MaterialId = entity.MaterialId,
                        MaterialCode = entity.MaterialCode,
                        MaterialName = entity.MaterialName,
                        BatchNumber = entity.BatchNumber,
                        Quantity = entity.RejectedQuantity > 0 ? entity.RejectedQuantity : entity.SamplingQuantity,
                        Reason = remarks?.Trim() ?? entity.Remarks?.Trim() ?? "Incoming inspection rejected",
                        Department = "Incoming QC",
                        Operator = entity.Inspector,
                        SupplierName = entity.SupplierName,
                        Remarks = $"Auto-created from {entity.InspectionNumber}"
                    };
                    await RecordRejectionAsync(rejRequest, actingUser, cancellationToken);
                }
            }

            return (await GetIncomingByIdAsync(id, cancellationToken))!;
        }

        // ── In-Process QC ──

        public async Task<PagedResult<InProcessListItemDto>> GetInProcessChecksAsync(ListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _db.InProcessChecks.AsNoTracking().Where(x => !x.IsDeleted);
            var total = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 1000);

            var list = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
            var dtos = list.Select(MapToInProcessListItem).ToList();
            return PagedResult<InProcessListItemDto>.Create(dtos, total, page, size);
        }

        public async Task<InProcessDto?> GetInProcessByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.InProcessChecks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return entity is null ? null : MapToInProcessDto(entity);
        }

        public async Task<InProcessDto> CreateInProcessAsync(InProcessCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Operator))
            {
                throw new InvalidOperationException("Operator is required.");
            }

            var entry = await _db.ProductionEntries.FirstOrDefaultAsync(x => x.EntryNumber == request.ProductionEntryNumber.Trim() && !x.IsDeleted, cancellationToken);
            if (entry is null) throw new InvalidOperationException("Production Entry not found.");

            var wo = await _db.WorkOrders.FirstOrDefaultAsync(x => x.WorkOrderNumber == request.WorkOrderNumber.Trim() && !x.IsDeleted, cancellationToken);
            if (wo is null) throw new InvalidOperationException("Work Order not found.");

            var machine = await _db.Machines.FirstOrDefaultAsync(x => x.MachineCode == request.MachineCode.Trim() && !x.IsDeleted, cancellationToken);
            if (machine is null) throw new InvalidOperationException("Machine not found.");

            var product = await _db.FinishedGoods.FirstOrDefaultAsync(x => x.ProductCode == request.ProductCode.Trim() && !x.IsDeleted, cancellationToken);
            if (product is null) throw new InvalidOperationException("Product not found.");

            var now = DateTime.UtcNow;
            var num = await _numberingService.NextNumberAsync("QC", cancellationToken);

            var entity = new InProcessCheck
            {
                QCNumber = num,
                ProductionEntryId = entry.Id,
                ProductionEntryNumber = request.ProductionEntryNumber.Trim(),
                WorkOrderId = wo.Id,
                WorkOrderNumber = request.WorkOrderNumber.Trim(),
                MachineId = machine.Id,
                MachineCode = request.MachineCode.Trim(),
                MachineName = request.MachineName.Trim(),
                Operator = request.Operator.Trim(),
                Shift = request.Shift,
                Stage = request.Stage.Trim(),
                ProductId = product.Id,
                ProductCode = request.ProductCode.Trim(),
                ProductName = request.ProductName.Trim(),
                BatchNumber = request.BatchNumber.Trim(),
                Parameter = request.Parameter.Trim(),
                Tolerance = request.Tolerance.Trim(),
                ExpectedValue = request.ExpectedValue.Trim(),
                ActualValue = request.ActualValue.Trim(),
                Result = QcCheckResult.Pending,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                Status = InProcessQcStatus.Draft,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            _db.InProcessChecks.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToInProcessDto(entity);
        }

        public async Task<InProcessDto?> UpdateInProcessAsync(int id, InProcessCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.InProcessChecks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (entity.Status != InProcessQcStatus.Draft)
            {
                throw new InvalidOperationException("Only draft QC checks can be edited.");
            }

            if (string.IsNullOrWhiteSpace(request.Operator))
            {
                throw new InvalidOperationException("Operator is required.");
            }

            var entry = await _db.ProductionEntries.FirstOrDefaultAsync(x => x.EntryNumber == request.ProductionEntryNumber.Trim() && !x.IsDeleted, cancellationToken);
            if (entry is null) throw new InvalidOperationException("Production Entry not found.");

            var wo = await _db.WorkOrders.FirstOrDefaultAsync(x => x.WorkOrderNumber == request.WorkOrderNumber.Trim() && !x.IsDeleted, cancellationToken);
            if (wo is null) throw new InvalidOperationException("Work Order not found.");

            var machine = await _db.Machines.FirstOrDefaultAsync(x => x.MachineCode == request.MachineCode.Trim() && !x.IsDeleted, cancellationToken);
            if (machine is null) throw new InvalidOperationException("Machine not found.");

            var product = await _db.FinishedGoods.FirstOrDefaultAsync(x => x.ProductCode == request.ProductCode.Trim() && !x.IsDeleted, cancellationToken);
            if (product is null) throw new InvalidOperationException("Product not found.");

            entity.ProductionEntryId = entry.Id;
            entity.ProductionEntryNumber = request.ProductionEntryNumber.Trim();
            entity.WorkOrderId = wo.Id;
            entity.WorkOrderNumber = request.WorkOrderNumber.Trim();
            entity.MachineId = machine.Id;
            entity.MachineCode = request.MachineCode.Trim();
            entity.MachineName = request.MachineName.Trim();
            entity.Operator = request.Operator.Trim();
            entity.Shift = request.Shift;
            entity.Stage = request.Stage.Trim();
            entity.ProductId = product.Id;
            entity.ProductCode = request.ProductCode.Trim();
            entity.ProductName = request.ProductName.Trim();
            entity.BatchNumber = request.BatchNumber.Trim();
            entity.Parameter = request.Parameter.Trim();
            entity.Tolerance = request.Tolerance.Trim();
            entity.ExpectedValue = request.ExpectedValue.Trim();
            entity.ActualValue = request.ActualValue.Trim();
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return MapToInProcessDto(entity);
        }

        public async Task<bool> DeleteInProcessAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.InProcessChecks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            if (entity.Status != InProcessQcStatus.Draft)
            {
                throw new InvalidOperationException("Only draft QC checks can be deleted.");
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<InProcessDto?> StartInProcessAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeInProcessStatusAsync(id, InProcessQcStatus.Running, payload?.Remarks, actingUser, cancellationToken);

        public async Task<InProcessDto?> RecordInProcessResultAsync(int id, string? actualValue, string? result, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.InProcessChecks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (entity.Status != InProcessQcStatus.Running)
            {
                throw new InvalidOperationException("Results can only be recorded when QC check is in Running status.");
            }

            if (!string.IsNullOrWhiteSpace(actualValue)) entity.ActualValue = actualValue.Trim();
            if (!string.IsNullOrWhiteSpace(result) && Enum.TryParse<QcCheckResult>(result, true, out var rEnum))
            {
                entity.Result = rEnum;
                if (rEnum == QcCheckResult.Pass)
                {
                    entity.Status = InProcessQcStatus.Passed;
                }
                else if (rEnum == QcCheckResult.Fail)
                {
                    entity.Status = InProcessQcStatus.Failed;
                }
            }

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);

            if (entity.Status == InProcessQcStatus.Failed)
            {
                var existingRejection = await _db.RejectionAnalyses.FirstOrDefaultAsync(
                    x => x.Source == RejectionSource.InProcess && x.SourceRecordId == entity.Id && !x.IsDeleted,
                    cancellationToken);

                if (existingRejection is null)
                {
                    var rejRequest = new RejectionCreateRequestDto
                    {
                        Source = RejectionSource.InProcess,
                        SourceRecordId = entity.Id,
                        SourceRecordNumber = entity.QCNumber,
                        ProductId = entity.ProductId,
                        ProductCode = entity.ProductCode,
                        ProductName = entity.ProductName,
                        BatchNumber = entity.BatchNumber,
                        Quantity = 1,
                        Reason = entity.Remarks?.Trim() ?? $"{entity.Parameter} failed",
                        Department = "Production",
                        Operator = entity.Operator,
                        MachineId = entity.MachineId,
                        MachineCode = entity.MachineCode,
                        MachineName = entity.MachineName,
                        Remarks = $"Auto-created from {entity.QCNumber}"
                    };
                    await RecordRejectionAsync(rejRequest, actingUser, cancellationToken);
                }
            }

            return MapToInProcessDto(entity);
        }

        public async Task<InProcessDto?> PassInProcessAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.InProcessChecks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity != null) entity.Result = QcCheckResult.Pass;
            return await ChangeInProcessStatusAsync(id, InProcessQcStatus.Passed, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<InProcessDto?> FailInProcessAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.InProcessChecks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity != null) entity.Result = QcCheckResult.Fail;
            return await ChangeInProcessStatusAsync(id, InProcessQcStatus.Failed, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<InProcessDto?> CloseInProcessAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeInProcessStatusAsync(id, InProcessQcStatus.Closed, payload?.Remarks, actingUser, cancellationToken);

        public async Task<InProcessDashboardDto> GetInProcessDashboardAsync(CancellationToken cancellationToken = default)
        {
            var q = _db.InProcessChecks.AsNoTracking().Where(x => !x.IsDeleted);

            return new InProcessDashboardDto
            {
                RunningChecks = await q.CountAsync(x => x.Status == InProcessQcStatus.Running, cancellationToken),
                Passed = await q.CountAsync(x => x.Status == InProcessQcStatus.Passed, cancellationToken),
                Failed = await q.CountAsync(x => x.Status == InProcessQcStatus.Failed, cancellationToken),
                Pending = await q.CountAsync(x => x.Status == InProcessQcStatus.Draft, cancellationToken)
            };
        }

        private async Task<InProcessDto?> ChangeInProcessStatusAsync(int id, InProcessQcStatus target, string? remarks, string actingUser, CancellationToken cancellationToken)
        {
            var entity = await _db.InProcessChecks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (!QualityControlRules.CanTransitionInProcess(entity.Status, target))
            {
                throw new InvalidOperationException($"Cannot transition in-process check from {entity.Status} to {target}.");
            }

            entity.Status = target;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            if (!string.IsNullOrWhiteSpace(remarks)) entity.Remarks = remarks.Trim();

            await _db.SaveChangesAsync(cancellationToken);

            if (target == InProcessQcStatus.Failed)
            {
                var existingRejection = await _db.RejectionAnalyses.FirstOrDefaultAsync(
                    x => x.Source == RejectionSource.InProcess && x.SourceRecordId == entity.Id && !x.IsDeleted,
                    cancellationToken);

                if (existingRejection is null)
                {
                    var rejRequest = new RejectionCreateRequestDto
                    {
                        Source = RejectionSource.InProcess,
                        SourceRecordId = entity.Id,
                        SourceRecordNumber = entity.QCNumber,
                        ProductId = entity.ProductId,
                        ProductCode = entity.ProductCode,
                        ProductName = entity.ProductName,
                        BatchNumber = entity.BatchNumber,
                        Quantity = 1,
                        Reason = remarks?.Trim() ?? entity.Remarks?.Trim() ?? $"{entity.Parameter} failed",
                        Department = "Production",
                        Operator = entity.Operator,
                        MachineId = entity.MachineId,
                        MachineCode = entity.MachineCode,
                        MachineName = entity.MachineName,
                        Remarks = $"Auto-created from {entity.QCNumber}"
                    };
                    await RecordRejectionAsync(rejRequest, actingUser, cancellationToken);
                }
            }

            return MapToInProcessDto(entity);
        }

        // ── Final Inspection ──

        public async Task<PagedResult<FinalListItemDto>> GetFinalInspectionsAsync(ListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _db.FinalInspections.AsNoTracking().Where(x => !x.IsDeleted);
            var total = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 1000);

            var list = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
            var dtos = list.Select(MapToFinalListItem).ToList();
            return PagedResult<FinalListItemDto>.Create(dtos, total, page, size);
        }

        public async Task<FinalDto?> GetFinalByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.FinalInspections.Include(x => x.Parameters).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return entity is null ? null : MapToFinalDto(entity);
        }

        public async Task<FinalDto> CreateFinalAsync(FinalCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Inspector))
            {
                throw new InvalidOperationException("Inspector is required.");
            }

            var product = await _db.FinishedGoods.FirstOrDefaultAsync(x => x.ProductCode == request.FinishedProductCode.Trim() && !x.IsDeleted, cancellationToken);
            if (product is null) throw new InvalidOperationException("Finished Product not found.");

            var entry = await _db.ProductionEntries.FirstOrDefaultAsync(x => x.EntryNumber == request.ProductionEntryNumber.Trim() && !x.IsDeleted, cancellationToken);
            if (entry is null) throw new InvalidOperationException("Production Entry not found.");

            if (entry.ProductId != product.Id)
            {
                throw new InvalidOperationException("Product does not match the source production record.");
            }

            if (request.AcceptedQuantity < 0 || request.RejectedQuantity < 0)
            {
                throw new InvalidOperationException("Quantities cannot be negative.");
            }

            if (request.AcceptedQuantity + request.RejectedQuantity > entry.GoodQuantity)
            {
                throw new InvalidOperationException("Accepted + rejected quantity cannot exceed production entry good quantity.");
            }

            var now = DateTime.UtcNow;
            var num = await _numberingService.NextNumberAsync("FINSP", cancellationToken);

            var entity = new FinalInspection
            {
                InspectionNumber = num,
                InspectionDate = request.InspectionDate.ToUniversalTime(),
                FinishedProductId = product.Id,
                FinishedProductCode = request.FinishedProductCode.Trim(),
                FinishedProductName = request.FinishedProductName.Trim(),
                ProductionEntryId = entry.Id,
                ProductionEntryNumber = request.ProductionEntryNumber.Trim(),
                ProductionBatch = request.ProductionBatch.Trim(),
                Inspector = request.Inspector.Trim(),
                Dimension = request.Dimension.Trim(),
                Weight = request.Weight.Trim(),
                Strength = request.Strength.Trim(),
                SurfaceFinish = request.SurfaceFinish.Trim(),
                VisualCheck = request.VisualCheck.Trim(),
                AcceptedQuantity = request.AcceptedQuantity,
                RejectedQuantity = request.RejectedQuantity,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                Status = FinalInspectionStatus.Draft,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            foreach (var p in request.Parameters ?? new())
            {
                entity.Parameters.Add(new FinalInspectionParameter
                {
                    Name = p.Name.Trim(),
                    Expected = p.Expected.Trim(),
                    Actual = p.Actual.Trim(),
                    Result = p.Result
                });
            }

            _db.FinalInspections.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetFinalByIdAsync(entity.Id, cancellationToken))!;
        }

        public async Task<FinalDto?> UpdateFinalAsync(int id, FinalCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.FinalInspections.Include(x => x.Parameters).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (entity.Status != FinalInspectionStatus.Draft)
            {
                throw new InvalidOperationException("Only draft final inspections can be edited.");
            }

            if (string.IsNullOrWhiteSpace(request.Inspector))
            {
                throw new InvalidOperationException("Inspector is required.");
            }

            var product = await _db.FinishedGoods.FirstOrDefaultAsync(x => x.ProductCode == request.FinishedProductCode.Trim() && !x.IsDeleted, cancellationToken);
            if (product is null) throw new InvalidOperationException("Finished Product not found.");

            var entry = await _db.ProductionEntries.FirstOrDefaultAsync(x => x.EntryNumber == request.ProductionEntryNumber.Trim() && !x.IsDeleted, cancellationToken);
            if (entry is null) throw new InvalidOperationException("Production Entry not found.");

            if (entry.ProductId != product.Id)
            {
                throw new InvalidOperationException("Product does not match the source production record.");
            }

            if (request.AcceptedQuantity < 0 || request.RejectedQuantity < 0)
            {
                throw new InvalidOperationException("Quantities cannot be negative.");
            }

            if (request.AcceptedQuantity + request.RejectedQuantity > entry.GoodQuantity)
            {
                throw new InvalidOperationException("Accepted + rejected quantity cannot exceed production entry good quantity.");
            }

            entity.InspectionDate = request.InspectionDate.ToUniversalTime();
            entity.FinishedProductId = product.Id;
            entity.FinishedProductCode = request.FinishedProductCode.Trim();
            entity.FinishedProductName = request.FinishedProductName.Trim();
            entity.ProductionEntryId = entry.Id;
            entity.ProductionEntryNumber = request.ProductionEntryNumber.Trim();
            entity.ProductionBatch = request.ProductionBatch.Trim();
            entity.Inspector = request.Inspector.Trim();
            entity.Dimension = request.Dimension.Trim();
            entity.Weight = request.Weight.Trim();
            entity.Strength = request.Strength.Trim();
            entity.SurfaceFinish = request.SurfaceFinish.Trim();
            entity.VisualCheck = request.VisualCheck.Trim();
            entity.AcceptedQuantity = request.AcceptedQuantity;
            entity.RejectedQuantity = request.RejectedQuantity;
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            _db.FinalInspectionParameters.RemoveRange(entity.Parameters);
            entity.Parameters.Clear();

            foreach (var p in request.Parameters ?? new())
            {
                entity.Parameters.Add(new FinalInspectionParameter
                {
                    FinalInspectionId = id,
                    Name = p.Name.Trim(),
                    Expected = p.Expected.Trim(),
                    Actual = p.Actual.Trim(),
                    Result = p.Result
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetFinalByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteFinalAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.FinalInspections.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<FinalDto?> SubmitFinalAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeFinalStatusAsync(id, FinalInspectionStatus.Submitted, payload?.Remarks, actingUser, cancellationToken);

        public async Task<FinalDto?> ApproveFinalAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeFinalStatusAsync(id, FinalInspectionStatus.Approved, payload?.Remarks, actingUser, cancellationToken);

        public async Task<FinalDto?> RejectFinalAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeFinalStatusAsync(id, FinalInspectionStatus.Rejected, payload?.Remarks, actingUser, cancellationToken);

        public async Task<FinalDto?> CloseFinalAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeFinalStatusAsync(id, FinalInspectionStatus.Closed, payload?.Remarks, actingUser, cancellationToken);

        public async Task<CertificateDto> GenerateCertificateAsync(int finalId, string actingUser, CancellationToken cancellationToken = default)
        {
            var fin = await _db.FinalInspections.FirstOrDefaultAsync(x => x.Id == finalId && !x.IsDeleted, cancellationToken);
            if (fin is null) throw new InvalidOperationException($"Final inspection {finalId} not found.");

            var certReq = new CertificateCreateRequestDto
            {
                CertificateDate = DateTime.UtcNow,
                CustomerId = 1,
                CustomerName = "Standard Customer",
                ProductId = fin.FinishedProductId,
                ProductCode = fin.FinishedProductCode,
                ProductName = fin.FinishedProductName,
                FinalInspectionId = fin.Id,
                FinalInspectionNumber = fin.InspectionNumber,
                LoadTestId = fin.LoadTestId,
                BatchNumber = fin.ProductionBatch,
                IssuedBy = actingUser,
                Remarks = $"Auto-generated from Final Inspection {fin.InspectionNumber}"
            };

            var cert = await CreateCertificateAsync(certReq, actingUser, cancellationToken);
            fin.TestCertificateId = cert.Id;
            await _db.SaveChangesAsync(cancellationToken);

            return cert;
        }

        public async Task<LoadTestDto> GenerateLoadReportAsync(int finalId, string actingUser, CancellationToken cancellationToken = default)
        {
            var fin = await _db.FinalInspections.FirstOrDefaultAsync(x => x.Id == finalId && !x.IsDeleted, cancellationToken);
            if (fin is null) throw new InvalidOperationException($"Final inspection {finalId} not found.");

            var ltReq = new LoadTestCreateRequestDto
            {
                TestDate = DateTime.UtcNow,
                ProductId = fin.FinishedProductId,
                ProductCode = fin.FinishedProductCode,
                ProductName = fin.FinishedProductName,
                FinalInspectionId = fin.Id,
                FinalInspectionNumber = fin.InspectionNumber,
                MachineId = 1,
                MachineCode = "MCH-001",
                MachineName = "Universal Load Test Rig",
                LoadCapacity = 1000m,
                AppliedLoad = 500m,
                DurationMinutes = 30,
                Result = LoadTestStatus.Passed,
                Remarks = $"Auto-generated load test report for Final Inspection {fin.InspectionNumber}"
            };

            var lt = await CreateLoadTestAsync(ltReq, actingUser, cancellationToken);
            fin.LoadTestId = lt.Id;
            await _db.SaveChangesAsync(cancellationToken);

            return lt;
        }

        public async Task<FinalDashboardDto> GetFinalDashboardAsync(CancellationToken cancellationToken = default)
        {
            var q = _db.FinalInspections.AsNoTracking().Where(x => !x.IsDeleted);
            var today = DateTime.UtcNow.Date;

            return new FinalDashboardDto
            {
                TodaysInspection = await q.CountAsync(x => x.InspectionDate >= today, cancellationToken),
                Approved = await q.CountAsync(x => x.Status == FinalInspectionStatus.Approved, cancellationToken),
                Rejected = await q.CountAsync(x => x.Status == FinalInspectionStatus.Rejected, cancellationToken),
                Pending = await q.CountAsync(x => x.Status == FinalInspectionStatus.Draft || x.Status == FinalInspectionStatus.Submitted, cancellationToken)
            };
        }

        private async Task<FinalDto?> ChangeFinalStatusAsync(int id, FinalInspectionStatus target, string? remarks, string actingUser, CancellationToken cancellationToken)
        {
            var entity = await _db.FinalInspections.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (!QualityControlRules.CanTransitionFinal(entity.Status, target))
            {
                throw new InvalidOperationException($"Cannot transition final inspection from {entity.Status} to {target}.");
            }

            entity.Status = target;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            if (!string.IsNullOrWhiteSpace(remarks)) entity.Remarks = remarks.Trim();

            await _db.SaveChangesAsync(cancellationToken);

            if (target == FinalInspectionStatus.Rejected)
            {
                var rejRequest = new RejectionCreateRequestDto
                {
                    Source = RejectionSource.FinalInspection,
                    SourceRecordId = entity.Id,
                    SourceRecordNumber = entity.InspectionNumber,
                    ProductId = entity.FinishedProductId,
                    ProductCode = entity.FinishedProductCode,
                    ProductName = entity.FinishedProductName,
                    BatchNumber = entity.ProductionBatch,
                    Quantity = entity.RejectedQuantity > 0 ? entity.RejectedQuantity : 1,
                    Reason = remarks?.Trim() ?? entity.Remarks?.Trim() ?? "Final inspection rejected",
                    Department = "Final QC",
                    Operator = entity.Inspector,
                    Remarks = $"Auto-created from {entity.InspectionNumber}"
                };
                await RecordRejectionAsync(rejRequest, actingUser, cancellationToken);
            }

            return (await GetFinalByIdAsync(id, cancellationToken))!;
        }

        // ── Load Test ──

        public async Task<PagedResult<LoadTestListItemDto>> GetLoadTestsAsync(ListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _db.LoadTestReports.AsNoTracking().Where(x => !x.IsDeleted);
            var total = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 1000);

            var list = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
            var dtos = list.Select(MapToLoadTestListItem).ToList();
            return PagedResult<LoadTestListItemDto>.Create(dtos, total, page, size);
        }

        public async Task<LoadTestDto?> GetLoadTestByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.LoadTestReports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return entity is null ? null : MapToLoadTestDto(entity);
        }

        public async Task<LoadTestDto> CreateLoadTestAsync(LoadTestCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var num = await _numberingService.NextNumberAsync("LTR", cancellationToken);

            var entity = new LoadTestReport
            {
                ReportNumber = num,
                TestDate = request.TestDate.ToUniversalTime(),
                ProductId = request.ProductId,
                ProductCode = request.ProductCode.Trim(),
                ProductName = request.ProductName.Trim(),
                FinalInspectionId = request.FinalInspectionId,
                FinalInspectionNumber = request.FinalInspectionNumber.Trim(),
                MachineId = request.MachineId,
                MachineCode = request.MachineCode.Trim(),
                MachineName = request.MachineName.Trim(),
                LoadCapacity = request.LoadCapacity,
                AppliedLoad = request.AppliedLoad,
                DurationMinutes = request.DurationMinutes,
                Result = request.Result,
                PassFail = request.Result == LoadTestStatus.Passed ? "Pass" : request.Result == LoadTestStatus.Failed ? "Fail" : "Pending",
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            _db.LoadTestReports.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToLoadTestDto(entity);
        }

        public async Task<LoadTestDashboardDto> GetLoadTestDashboardAsync(CancellationToken cancellationToken = default)
        {
            var q = _db.LoadTestReports.AsNoTracking().Where(x => !x.IsDeleted);

            return new LoadTestDashboardDto
            {
                TotalTests = await q.CountAsync(cancellationToken),
                Passed = await q.CountAsync(x => x.Result == LoadTestStatus.Passed, cancellationToken),
                Failed = await q.CountAsync(x => x.Result == LoadTestStatus.Failed, cancellationToken),
                Pending = await q.CountAsync(x => x.Result == LoadTestStatus.Pending, cancellationToken)
            };
        }

        // ── Test Certificate ──

        public async Task<PagedResult<CertificateListItemDto>> GetCertificatesAsync(ListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _db.TestCertificates.AsNoTracking().Where(x => !x.IsDeleted);
            var total = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 1000);

            var list = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
            var dtos = list.Select(MapToCertificateListItem).ToList();
            return PagedResult<CertificateListItemDto>.Create(dtos, total, page, size);
        }

        public async Task<CertificateDto?> GetCertificateByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.TestCertificates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return entity is null ? null : MapToCertificateDto(entity);
        }

        public async Task<CertificateDto> CreateCertificateAsync(CertificateCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var num = await _numberingService.NextNumberAsync("TC", cancellationToken);

            var entity = new TestCertificate
            {
                CertificateNumber = num,
                CertificateDate = request.CertificateDate.ToUniversalTime(),
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName.Trim(),
                ProductId = request.ProductId,
                ProductCode = request.ProductCode.Trim(),
                ProductName = request.ProductName.Trim(),
                FinalInspectionId = request.FinalInspectionId,
                FinalInspectionNumber = request.FinalInspectionNumber.Trim(),
                LoadTestId = request.LoadTestId,
                LoadTestNumber = request.LoadTestNumber?.Trim(),
                BatchNumber = request.BatchNumber.Trim(),
                IssuedBy = string.IsNullOrWhiteSpace(request.IssuedBy) ? actingUser : request.IssuedBy.Trim(),
                ExpiryDate = request.ExpiryDate?.ToUniversalTime(),
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                Status = CertificateStatus.Draft,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            _db.TestCertificates.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToCertificateDto(entity);
        }

        public async Task<CertificateDto?> UpdateCertificateAsync(int id, CertificateCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.TestCertificates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            entity.CertificateDate = request.CertificateDate.ToUniversalTime();
            entity.CustomerId = request.CustomerId;
            entity.CustomerName = request.CustomerName.Trim();
            entity.ProductId = request.ProductId;
            entity.ProductCode = request.ProductCode.Trim();
            entity.ProductName = request.ProductName.Trim();
            entity.FinalInspectionId = request.FinalInspectionId;
            entity.FinalInspectionNumber = request.FinalInspectionNumber.Trim();
            entity.LoadTestId = request.LoadTestId;
            entity.LoadTestNumber = request.LoadTestNumber?.Trim();
            entity.BatchNumber = request.BatchNumber.Trim();
            entity.IssuedBy = string.IsNullOrWhiteSpace(request.IssuedBy) ? entity.IssuedBy : request.IssuedBy.Trim();
            entity.ExpiryDate = request.ExpiryDate?.ToUniversalTime();
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return MapToCertificateDto(entity);
        }

        public async Task<bool> DeleteCertificateAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.TestCertificates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<CertificateDto?> DuplicateCertificateAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var src = await _db.TestCertificates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (src is null) return null;

            var req = new CertificateCreateRequestDto
            {
                CertificateDate = DateTime.UtcNow,
                CustomerId = src.CustomerId,
                CustomerName = src.CustomerName,
                ProductId = src.ProductId,
                ProductCode = src.ProductCode,
                ProductName = src.ProductName,
                FinalInspectionId = src.FinalInspectionId,
                FinalInspectionNumber = src.FinalInspectionNumber,
                LoadTestId = src.LoadTestId,
                LoadTestNumber = src.LoadTestNumber,
                BatchNumber = src.BatchNumber,
                IssuedBy = actingUser,
                ExpiryDate = src.ExpiryDate,
                Remarks = $"Duplicate of {src.CertificateNumber}",
                Notes = src.Notes
            };

            return await CreateCertificateAsync(req, actingUser, cancellationToken);
        }

        public async Task<CertificateDto?> ApproveCertificateAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            var entity = await _db.TestCertificates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity != null) entity.ApprovedBy = payload?.ApprovedBy ?? actingUser;
            return await ChangeCertificateStatusAsync(id, CertificateStatus.Approved, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<CertificateDto?> IssueCertificateAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeCertificateStatusAsync(id, CertificateStatus.Issued, payload?.Remarks, actingUser, cancellationToken);

        public async Task<CertificateDto?> CancelCertificateAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeCertificateStatusAsync(id, CertificateStatus.Cancelled, payload?.Remarks, actingUser, cancellationToken);

        public async Task<CertificateDashboardDto> GetCertificateDashboardAsync(CancellationToken cancellationToken = default)
        {
            var q = _db.TestCertificates.AsNoTracking().Where(x => !x.IsDeleted);

            return new CertificateDashboardDto
            {
                IssuedCertificates = await q.CountAsync(x => x.Status == CertificateStatus.Issued, cancellationToken),
                PendingApproval = await q.CountAsync(x => x.Status == CertificateStatus.Draft || x.Status == CertificateStatus.Approved, cancellationToken),
                Expired = await q.CountAsync(x => x.Status == CertificateStatus.Expired, cancellationToken),
                Total = await q.CountAsync(cancellationToken)
            };
        }

        private async Task<CertificateDto?> ChangeCertificateStatusAsync(int id, CertificateStatus target, string? remarks, string actingUser, CancellationToken cancellationToken)
        {
            var entity = await _db.TestCertificates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (!QualityControlRules.CanTransitionCertificate(entity.Status, target))
            {
                throw new InvalidOperationException($"Cannot transition certificate from {entity.Status} to {target}.");
            }

            entity.Status = target;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            if (!string.IsNullOrWhiteSpace(remarks)) entity.Remarks = remarks.Trim();

            await _db.SaveChangesAsync(cancellationToken);
            return MapToCertificateDto(entity);
        }

        // ── Rejection Analysis ──

        public async Task<PagedResult<RejectionListItemDto>> GetRejectionsAsync(ListQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _db.RejectionAnalyses.AsNoTracking().Where(x => !x.IsDeleted);
            var total = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 1000);

            var list = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
            var dtos = list.Select(MapToRejectionListItem).ToList();
            return PagedResult<RejectionListItemDto>.Create(dtos, total, page, size);
        }

        public async Task<RejectionDto?> GetRejectionByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.RejectionAnalyses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            return entity is null ? null : MapToRejectionDto(entity);
        }

        public async Task<RejectionDto> RecordRejectionAsync(RejectionCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            if (request.Quantity <= 0)
            {
                throw new InvalidOperationException("Quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("Reason is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Department))
            {
                throw new InvalidOperationException("Department is required.");
            }

            int? resolvedMachineId = request.MachineId;
            string? resolvedMachineCode = request.MachineCode;
            string? resolvedMachineName = request.MachineName;
            if (!string.IsNullOrWhiteSpace(request.MachineCode))
            {
                var machine = await _db.Machines.FirstOrDefaultAsync(x => x.MachineCode == request.MachineCode.Trim() && !x.IsDeleted, cancellationToken);
                if (machine is null) throw new InvalidOperationException("Machine not found.");
                resolvedMachineId = machine.Id;
                resolvedMachineCode = machine.MachineCode;
                resolvedMachineName = machine.MachineName;
            }

            int? resolvedProductId = request.ProductId;
            string? resolvedProductCode = request.ProductCode;
            string? resolvedProductName = request.ProductName;

            int? resolvedMaterialId = request.MaterialId;
            string? resolvedMaterialCode = request.MaterialCode;
            string? resolvedMaterialName = request.MaterialName;
            string? resolvedSupplierName = request.SupplierName;

            if (request.Source == RejectionSource.Incoming)
            {
                var incoming = await _db.IncomingInspections.FirstOrDefaultAsync(x => x.InspectionNumber == request.SourceRecordNumber.Trim() && !x.IsDeleted, cancellationToken);
                if (incoming is null) throw new InvalidOperationException("Source record not found.");

                resolvedMaterialId = incoming.MaterialId;
                resolvedMaterialCode = incoming.MaterialCode;
                resolvedMaterialName = incoming.MaterialName;
                resolvedSupplierName = incoming.SupplierName;

                if (request.Quantity > incoming.RejectedQuantity && incoming.RejectedQuantity > 0)
                {
                    throw new InvalidOperationException("Rejection quantity cannot exceed source inspection rejected quantity.");
                }
            }
            else if (request.Source == RejectionSource.InProcess)
            {
                var inprocess = await _db.InProcessChecks.FirstOrDefaultAsync(x => x.QCNumber == request.SourceRecordNumber.Trim() && !x.IsDeleted, cancellationToken);
                if (inprocess is null) throw new InvalidOperationException("Source record not found.");

                resolvedProductId = inprocess.ProductId;
                resolvedProductCode = inprocess.ProductCode;
                resolvedProductName = inprocess.ProductName;

                var wo = await _db.WorkOrders.FirstOrDefaultAsync(x => x.Id == inprocess.WorkOrderId && !x.IsDeleted, cancellationToken);
                if (wo is null) throw new InvalidOperationException("Work Order not found.");

                if (request.Quantity > 1)
                {
                    throw new InvalidOperationException("Rejection quantity cannot exceed applicable production quantity.");
                }
            }
            else if (request.Source == RejectionSource.FinalInspection)
            {
                var final = await _db.FinalInspections.FirstOrDefaultAsync(x => x.InspectionNumber == request.SourceRecordNumber.Trim() && !x.IsDeleted, cancellationToken);
                if (final is null) throw new InvalidOperationException("Source record not found.");

                resolvedProductId = final.FinishedProductId;
                resolvedProductCode = final.FinishedProductCode;
                resolvedProductName = final.FinishedProductName;

                var entry = await _db.ProductionEntries.FirstOrDefaultAsync(x => x.Id == final.ProductionEntryId && !x.IsDeleted, cancellationToken);
                if (entry is null) throw new InvalidOperationException("Production Entry not found.");

                var wo = await _db.WorkOrders.FirstOrDefaultAsync(x => x.Id == entry.WorkOrderId && !x.IsDeleted, cancellationToken);
                if (wo is null) throw new InvalidOperationException("Work Order not found.");

                if (request.Quantity > final.RejectedQuantity)
                {
                    throw new InvalidOperationException("Rejection quantity cannot exceed final inspection rejected quantity.");
                }
            }

            var now = DateTime.UtcNow;
            var num = await _numberingService.NextNumberAsync("REJ", cancellationToken);

            var entity = new RejectionAnalysis
            {
                RejectionNumber = num,
                RejectionDate = DateTime.UtcNow,
                Source = request.Source,
                SourceRecordId = request.SourceRecordId > 0 ? request.SourceRecordId : 0,
                SourceRecordNumber = request.SourceRecordNumber.Trim(),
                MaterialId = resolvedMaterialId,
                MaterialCode = resolvedMaterialCode,
                MaterialName = resolvedMaterialName,
                ProductId = resolvedProductId,
                ProductCode = resolvedProductCode,
                ProductName = resolvedProductName,
                BatchNumber = request.BatchNumber.Trim(),
                Quantity = request.Quantity,
                Reason = request.Reason.Trim(),
                RootCause = request.RootCause?.Trim() ?? string.Empty,
                Department = request.Department.Trim(),
                Operator = request.Operator?.Trim() ?? string.Empty,
                MachineId = resolvedMachineId,
                MachineCode = resolvedMachineCode,
                MachineName = resolvedMachineName,
                SupplierName = resolvedSupplierName,
                CorrectiveAction = request.CorrectiveAction?.Trim() ?? string.Empty,
                PreventiveAction = request.PreventiveAction?.Trim() ?? string.Empty,
                Status = RejectionAnalysisStatus.Open,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            _db.RejectionAnalyses.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToRejectionDto(entity);
        }

        public async Task<RejectionDto?> StartRejectionAnalysisAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeRejectionStatusAsync(id, RejectionAnalysisStatus.UnderAnalysis, payload?.Remarks, actingUser, cancellationToken);

        public async Task<RejectionDto?> CloseRejectionAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
            => await ChangeRejectionStatusAsync(id, RejectionAnalysisStatus.Closed, payload?.Remarks, actingUser, cancellationToken);

        public async Task<RejectionDashboardDto> GetRejectionDashboardAsync(CancellationToken cancellationToken = default)
        {
            var q = _db.RejectionAnalyses.AsNoTracking().Where(x => !x.IsDeleted);
            var total = await q.CountAsync(cancellationToken);
            var totalQty = await q.SumAsync(x => x.Quantity, cancellationToken);

            var topReasons = await q.GroupBy(x => x.Reason).Select(g => new NameCountDto { Reason = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(5).ToListAsync(cancellationToken);
            var deptWise = await q.GroupBy(x => x.Department).Select(g => new DepartmentCountDto { Department = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ToListAsync(cancellationToken);
            var suppWise = await q.Where(x => x.SupplierName != null).GroupBy(x => x.SupplierName!).Select(g => new SupplierCountDto { Supplier = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ToListAsync(cancellationToken);
            var machWise = await q.Where(x => x.MachineName != null).GroupBy(x => x.MachineName!).Select(g => new MachineCountDto { Machine = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ToListAsync(cancellationToken);

            return new RejectionDashboardDto
            {
                TotalRejections = total,
                RejectionPercent = total > 0 ? Math.Round((decimal)totalQty / total, 2) : 0m,
                TopReasons = topReasons,
                DepartmentWise = deptWise,
                SupplierWise = suppWise,
                MachineWise = machWise
            };
        }

        public async Task<RejectionReportDto> GetRejectionReportAsync(CancellationToken cancellationToken = default)
        {
            var q = _db.RejectionAnalyses.AsNoTracking().Where(x => !x.IsDeleted);
            var total = await q.CountAsync(cancellationToken);

            var bySource = await q.GroupBy(x => x.Source).Select(g => new SourceReportItemDto { Source = g.Key.ToString(), Count = g.Count(), Quantity = g.Sum(x => x.Quantity) }).ToListAsync(cancellationToken);
            var byReason = await q.GroupBy(x => x.Reason).Select(g => new NameCountDto { Reason = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
            var byDept = await q.GroupBy(x => x.Department).Select(g => new DepartmentCountDto { Department = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);

            return new RejectionReportDto
            {
                GeneratedAt = DateTime.UtcNow,
                TotalRejections = total,
                BySource = bySource,
                ByReason = byReason,
                ByDepartment = byDept,
                OpenCount = await q.CountAsync(x => x.Status == RejectionAnalysisStatus.Open || x.Status == RejectionAnalysisStatus.UnderAnalysis, cancellationToken),
                ClosedCount = await q.CountAsync(x => x.Status == RejectionAnalysisStatus.Closed, cancellationToken)
            };
        }

        private async Task<RejectionDto?> ChangeRejectionStatusAsync(int id, RejectionAnalysisStatus target, string? remarks, string actingUser, CancellationToken cancellationToken)
        {
            var entity = await _db.RejectionAnalyses.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return null;

            if (!QualityControlRules.CanTransitionRejection(entity.Status, target))
            {
                throw new InvalidOperationException($"Cannot transition rejection from {entity.Status} to {target}.");
            }

            entity.Status = target;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = actingUser;

            if (!string.IsNullOrWhiteSpace(remarks)) entity.Remarks = remarks.Trim();

            await _db.SaveChangesAsync(cancellationToken);
            return MapToRejectionDto(entity);
        }

        // ── Helpers ──

        private static InspectionResult ResolveIncomingResult(decimal accepted, decimal rejected, decimal sampling)
        {
            if (accepted == 0 && rejected == 0) return InspectionResult.Pending;
            if (rejected == 0) return InspectionResult.Accepted;
            if (accepted == 0) return InspectionResult.Rejected;
            return InspectionResult.Conditional;
        }

        private static IncomingListItemDto MapToIncomingListItem(IncomingInspection x) => new()
        {
            Id = x.Id,
            InspectionNumber = x.InspectionNumber,
            InspectionDate = x.InspectionDate,
            SupplierName = x.SupplierName,
            VendorName = x.VendorName,
            PurchaseOrderNumber = x.PurchaseOrderNumber,
            GRNNumber = x.GRNNumber,
            MaterialCode = x.MaterialCode,
            MaterialName = x.MaterialName,
            BatchNumber = x.BatchNumber,
            WarehouseName = x.WarehouseName,
            Inspector = x.Inspector,
            AcceptedQuantity = x.AcceptedQuantity,
            RejectedQuantity = x.RejectedQuantity,
            InspectionResult = x.InspectionResult,
            Status = x.Status
        };

        private static IncomingDto MapToIncomingDto(IncomingInspection x) => new()
        {
            Id = x.Id,
            InspectionNumber = x.InspectionNumber,
            InspectionDate = x.InspectionDate,
            SupplierId = x.SupplierId,
            SupplierName = x.SupplierName,
            VendorId = x.VendorId,
            VendorName = x.VendorName,
            PurchaseOrderId = x.PurchaseOrderId,
            PurchaseOrderNumber = x.PurchaseOrderNumber,
            GRNId = x.GRNId,
            GRNNumber = x.GRNNumber,
            MaterialId = x.MaterialId,
            MaterialCode = x.MaterialCode,
            MaterialName = x.MaterialName,
            BatchNumber = x.BatchNumber,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.WarehouseName,
            Inspector = x.Inspector,
            InspectionType = x.InspectionType,
            InspectionMethod = x.InspectionMethod,
            SamplingQuantity = x.SamplingQuantity,
            AcceptedQuantity = x.AcceptedQuantity,
            RejectedQuantity = x.RejectedQuantity,
            PendingQuantity = x.PendingQuantity,
            InspectionResult = x.InspectionResult,
            Checklist = x.Checklist?.Select(c => new QcChecklistItemDto
            {
                Id = c.Id.ToString(),
                Parameter = c.Parameter,
                Specification = c.Specification,
                ActualValue = c.ActualValue,
                Result = c.Result,
                Remarks = c.Remarks
            }).ToList() ?? new(),
            Remarks = x.Remarks,
            Notes = x.Notes,
            Status = x.Status,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            UpdatedBy = x.UpdatedBy,
            UpdatedAt = x.UpdatedAt
        };

        private static InProcessListItemDto MapToInProcessListItem(InProcessCheck x) => new()
        {
            Id = x.Id,
            QCNumber = x.QCNumber,
            ProductionEntryNumber = x.ProductionEntryNumber,
            WorkOrderNumber = x.WorkOrderNumber,
            MachineName = x.MachineName,
            Operator = x.Operator,
            Shift = x.Shift,
            Stage = x.Stage,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            BatchNumber = x.BatchNumber,
            Parameter = x.Parameter,
            Result = x.Result,
            Status = x.Status
        };

        private static InProcessDto MapToInProcessDto(InProcessCheck x) => new()
        {
            Id = x.Id,
            QCNumber = x.QCNumber,
            ProductionEntryId = x.ProductionEntryId,
            ProductionEntryNumber = x.ProductionEntryNumber,
            WorkOrderId = x.WorkOrderId,
            WorkOrderNumber = x.WorkOrderNumber,
            MachineId = x.MachineId,
            MachineCode = x.MachineCode,
            MachineName = x.MachineName,
            Operator = x.Operator,
            Shift = x.Shift,
            Stage = x.Stage,
            ProductId = x.ProductId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            BatchNumber = x.BatchNumber,
            Parameter = x.Parameter,
            Tolerance = x.Tolerance,
            ExpectedValue = x.ExpectedValue,
            ActualValue = x.ActualValue,
            Result = x.Result,
            Remarks = x.Remarks,
            Notes = x.Notes,
            Status = x.Status,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            UpdatedBy = x.UpdatedBy,
            UpdatedAt = x.UpdatedAt
        };

        private static FinalListItemDto MapToFinalListItem(FinalInspection x) => new()
        {
            Id = x.Id,
            InspectionNumber = x.InspectionNumber,
            InspectionDate = x.InspectionDate,
            FinishedProductCode = x.FinishedProductCode,
            FinishedProductName = x.FinishedProductName,
            ProductionEntryNumber = x.ProductionEntryNumber,
            ProductionBatch = x.ProductionBatch,
            Inspector = x.Inspector,
            AcceptedQuantity = x.AcceptedQuantity,
            RejectedQuantity = x.RejectedQuantity,
            Status = x.Status
        };

        private static FinalDto MapToFinalDto(FinalInspection x) => new()
        {
            Id = x.Id,
            InspectionNumber = x.InspectionNumber,
            InspectionDate = x.InspectionDate,
            FinishedProductId = x.FinishedProductId,
            FinishedProductCode = x.FinishedProductCode,
            FinishedProductName = x.FinishedProductName,
            ProductionEntryId = x.ProductionEntryId,
            ProductionEntryNumber = x.ProductionEntryNumber,
            ProductionBatch = x.ProductionBatch,
            Inspector = x.Inspector,
            Dimension = x.Dimension,
            Weight = x.Weight,
            Strength = x.Strength,
            SurfaceFinish = x.SurfaceFinish,
            VisualCheck = x.VisualCheck,
            Parameters = x.Parameters?.Select(p => new FinalInspectionParameterDto
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Expected = p.Expected,
                Actual = p.Actual,
                Result = p.Result
            }).ToList() ?? new(),
            AcceptedQuantity = x.AcceptedQuantity,
            RejectedQuantity = x.RejectedQuantity,
            Remarks = x.Remarks,
            Notes = x.Notes,
            Status = x.Status,
            TestCertificateId = x.TestCertificateId,
            LoadTestId = x.LoadTestId,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            UpdatedBy = x.UpdatedBy,
            UpdatedAt = x.UpdatedAt
        };

        private static LoadTestListItemDto MapToLoadTestListItem(LoadTestReport x) => new()
        {
            Id = x.Id,
            ReportNumber = x.ReportNumber,
            TestDate = x.TestDate,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            FinalInspectionNumber = x.FinalInspectionNumber,
            MachineName = x.MachineName,
            LoadCapacity = x.LoadCapacity,
            AppliedLoad = x.AppliedLoad,
            Result = x.Result,
            PassFail = x.PassFail
        };

        private static LoadTestDto MapToLoadTestDto(LoadTestReport x) => new()
        {
            Id = x.Id,
            ReportNumber = x.ReportNumber,
            TestDate = x.TestDate,
            ProductId = x.ProductId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            FinalInspectionId = x.FinalInspectionId,
            FinalInspectionNumber = x.FinalInspectionNumber,
            MachineId = x.MachineId,
            MachineCode = x.MachineCode,
            MachineName = x.MachineName,
            LoadCapacity = x.LoadCapacity,
            AppliedLoad = x.AppliedLoad,
            DurationMinutes = x.DurationMinutes,
            Result = x.Result,
            PassFail = x.PassFail,
            Remarks = x.Remarks,
            Notes = x.Notes,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            UpdatedBy = x.UpdatedBy,
            UpdatedAt = x.UpdatedAt
        };

        private static CertificateListItemDto MapToCertificateListItem(TestCertificate x) => new()
        {
            Id = x.Id,
            CertificateNumber = x.CertificateNumber,
            CertificateDate = x.CertificateDate,
            CustomerName = x.CustomerName,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            FinalInspectionNumber = x.FinalInspectionNumber,
            BatchNumber = x.BatchNumber,
            IssuedBy = x.IssuedBy,
            Status = x.Status
        };

        private static CertificateDto MapToCertificateDto(TestCertificate x) => new()
        {
            Id = x.Id,
            CertificateNumber = x.CertificateNumber,
            CertificateDate = x.CertificateDate,
            CustomerId = x.CustomerId,
            CustomerName = x.CustomerName,
            ProductId = x.ProductId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            FinalInspectionId = x.FinalInspectionId,
            FinalInspectionNumber = x.FinalInspectionNumber,
            LoadTestId = x.LoadTestId,
            LoadTestNumber = x.LoadTestNumber,
            BatchNumber = x.BatchNumber,
            IssuedBy = x.IssuedBy,
            ApprovedBy = x.ApprovedBy,
            Status = x.Status,
            ExpiryDate = x.ExpiryDate,
            Remarks = x.Remarks,
            Notes = x.Notes,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            UpdatedBy = x.UpdatedBy,
            UpdatedAt = x.UpdatedAt
        };

        private static RejectionListItemDto MapToRejectionListItem(RejectionAnalysis x) => new()
        {
            Id = x.Id,
            RejectionNumber = x.RejectionNumber,
            RejectionDate = x.RejectionDate,
            Source = x.Source,
            SourceRecordNumber = x.SourceRecordNumber,
            MaterialName = x.MaterialName,
            ProductName = x.ProductName,
            BatchNumber = x.BatchNumber,
            Quantity = x.Quantity,
            Reason = x.Reason,
            Department = x.Department,
            Status = x.Status
        };

        private static RejectionDto MapToRejectionDto(RejectionAnalysis x) => new()
        {
            Id = x.Id,
            RejectionNumber = x.RejectionNumber,
            RejectionDate = x.RejectionDate,
            Source = x.Source,
            SourceRecordId = x.SourceRecordId,
            SourceRecordNumber = x.SourceRecordNumber,
            MaterialId = x.MaterialId,
            MaterialCode = x.MaterialCode,
            MaterialName = x.MaterialName,
            ProductId = x.ProductId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            BatchNumber = x.BatchNumber,
            Quantity = x.Quantity,
            Reason = x.Reason,
            RootCause = x.RootCause,
            Department = x.Department,
            Operator = x.Operator,
            MachineId = x.MachineId,
            MachineCode = x.MachineCode,
            MachineName = x.MachineName,
            SupplierName = x.SupplierName,
            CorrectiveAction = x.CorrectiveAction,
            PreventiveAction = x.PreventiveAction,
            Status = x.Status,
            Remarks = x.Remarks,
            Notes = x.Notes,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            UpdatedBy = x.UpdatedBy,
            UpdatedAt = x.UpdatedAt
        };

        public Task<IReadOnlyList<string>> GetPermissionsAsync()
        {
            IReadOnlyList<string> permissions = new List<string>
            {
                "quality-control.view",
                "quality-control.create",
                "quality-control.edit",
                "quality-control.delete",
                "quality-control.approve",
                "quality-control.reject",
                "quality-control.inspect",
                "quality-control.certificate",
                "quality-control.load-test",
                "quality-control.analysis.view",
                "quality-control.dashboard.view",
                "quality-control.export",
                "quality-control.print",
                "quality-control.audit.view"
            };
            return Task.FromResult(permissions);
        }
    }
}
