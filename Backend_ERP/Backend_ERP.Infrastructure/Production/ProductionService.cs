using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Production;
using ERP.Application.Production.Dtos;
using ERP.Domain.Procurement;
using ERP.Domain.Production;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Production
{
    public class ProductionService : IProductionService
    {
        private readonly ERPDbContext _dbContext;

        public ProductionService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ── DOCUMENT NUMBER GENERATOR ──

        private async Task<string> GenerateDocNumberAsync(string prefix, CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var fullPrefix = $"{prefix.ToUpperInvariant()}-{year}";

            var sequence = await _dbContext.ProductionDocumentSequences
                .FirstOrDefaultAsync(x => x.Prefix == fullPrefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new ProductionDocumentSequence
                {
                    Prefix = fullPrefix,
                    LastSequence = 0
                };
                _dbContext.ProductionDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return $"{fullPrefix}-{sequence.LastSequence:D6}";
        }

        // ── BILL OF MATERIALS (BOM) ──

        public async Task<List<BomListItemDto>> GetBomsAsync(string? search, string? status, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.BillOfMaterials
                .Include(x => x.Materials)
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.BomNumber.ToLower().Contains(s) ||
                                 x.ProductCode.ToLower().Contains(s) ||
                                 x.ProductName.ToLower().Contains(s) ||
                                 x.Revision.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<BomStatus>(status, true, out var statusEnum))
                {
                    q = q.Where(x => x.Status == statusEnum);
                }
            }

            var items = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
            return items.Select(MapToBomListItemDto).ToList();
        }

        public async Task<BomDto?> GetBomByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var bom = await _dbContext.BillOfMaterials
                .Include(x => x.Materials)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return bom is null ? null : MapToBomDto(bom);
        }

        public async Task<BomDto> CreateBomAsync(BomCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            if (request.Materials == null || request.Materials.Count == 0)
            {
                throw new InvalidOperationException("BOM must include at least one material");
            }

            var effectiveDate = DateOnly.Parse(request.EffectiveDate);
            DateOnly? expiryDate = string.IsNullOrWhiteSpace(request.ExpiryDate) ? null : DateOnly.Parse(request.ExpiryDate);

            if (expiryDate.HasValue && expiryDate.Value < effectiveDate)
            {
                throw new InvalidOperationException("Expiry date must be on or after effective date");
            }

            // Duplicate Check
            var codeKey = request.ProductCode.Trim().ToLower();
            var revKey = request.Revision.Trim().ToLower();
            var duplicate = await _dbContext.BillOfMaterials
                .AnyAsync(x => !x.IsDeleted &&
                               x.ProductCode.ToLower() == codeKey &&
                               x.Revision.ToLower() == revKey &&
                               x.Status != BomStatus.Archived, cancellationToken);

            if (duplicate)
            {
                throw new InvalidOperationException($"A BOM already exists for product {request.ProductCode.Trim()} revision {request.Revision.Trim()}");
            }

            // Resolve Product ID
            var product = await _dbContext.FinishedGoods
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProductCode.ToLower() == codeKey, cancellationToken);

            var productId = product?.Id ?? request.ProductId;
            if (productId == 0 && product == null)
            {
                throw new InvalidOperationException($"Product with code '{request.ProductCode}' not found.");
            }

            var bomNumber = await GenerateDocNumberAsync("BOM", cancellationToken);

            var bom = new BillOfMaterials
                {
                    BomNumber = bomNumber,
                    Version = "1.0",
                    ProductId = productId,
                    ProductCode = request.ProductCode.Trim(),
                    ProductName = request.ProductName.Trim(),
                    Revision = request.Revision.Trim(),
                    Status = BomStatus.Draft,
                    EffectiveDate = effectiveDate,
                    ExpiryDate = expiryDate,
                    ProcessNotes = request.ProcessNotes?.Trim() ?? string.Empty,
                    Notes = request.Notes?.Trim() ?? string.Empty,
                    CreatedBy = actingUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = actingUser,
                    UpdatedAt = DateTime.UtcNow
                };

                foreach (var m in request.Materials)
                {
                    var matCode = m.MaterialCode.Trim().ToLower();
                    var rawMaterial = await _dbContext.RawMaterials
                        .FirstOrDefaultAsync(x => !x.IsDeleted && x.MaterialCode.ToLower() == matCode, cancellationToken);

                    var matId = rawMaterial?.Id ?? m.MaterialId;
                    if (matId == 0 && rawMaterial == null)
                    {
                        throw new InvalidOperationException($"Raw material with code '{m.MaterialCode}' not found.");
                    }

                    bom.Materials.Add(new BomMaterialLine
                    {
                        MaterialId = matId,
                        MaterialCode = m.MaterialCode.Trim(),
                        MaterialName = m.MaterialName.Trim(),
                        Quantity = m.Quantity,
                        Uom = string.IsNullOrWhiteSpace(m.Uom) ? "Nos" : m.Uom.Trim(),
                        WastagePercent = m.WastagePercent,
                        WarehouseId = m.WarehouseId == 0 ? 1 : m.WarehouseId,
                        WarehouseName = string.IsNullOrWhiteSpace(m.WarehouseName) ? "Main RM Store" : m.WarehouseName.Trim()
                    });
                }

                _dbContext.BillOfMaterials.Add(bom);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return MapToBomDto(bom);
        }

        public async Task<BomDto?> UpdateBomAsync(int id, BomUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var bom = await _dbContext.BillOfMaterials
                .Include(x => x.Materials)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (bom is null) return null;

            if (bom.Status != BomStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft BOMs can be edited");
            }

            if (request.Materials == null || request.Materials.Count == 0)
            {
                throw new InvalidOperationException("BOM must include at least one material");
            }

            var effectiveDate = DateOnly.Parse(request.EffectiveDate);
            DateOnly? expiryDate = string.IsNullOrWhiteSpace(request.ExpiryDate) ? null : DateOnly.Parse(request.ExpiryDate);

            if (expiryDate.HasValue && expiryDate.Value < effectiveDate)
            {
                throw new InvalidOperationException("Expiry date must be on or after effective date");
            }

            var codeKey = request.ProductCode.Trim().ToLower();
            var revKey = request.Revision.Trim().ToLower();

            // Resolve Product ID
            var product = await _dbContext.FinishedGoods
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProductCode.ToLower() == codeKey, cancellationToken);

            var productId = product?.Id ?? request.ProductId;
            if (productId == 0 && product == null)
            {
                throw new InvalidOperationException($"Product with code '{request.ProductCode}' not found.");
            }

            bom.ProductId = productId;
            bom.ProductCode = request.ProductCode.Trim();
            bom.ProductName = request.ProductName.Trim();
            bom.Revision = request.Revision.Trim();
            bom.EffectiveDate = effectiveDate;
            bom.ExpiryDate = expiryDate;
            bom.ProcessNotes = request.ProcessNotes?.Trim() ?? string.Empty;
            bom.Notes = request.Notes?.Trim() ?? string.Empty;
            bom.UpdatedBy = actingUser;
            bom.UpdatedAt = DateTime.UtcNow;

            // Simple diff-based child updates
            _dbContext.BomMaterialLines.RemoveRange(bom.Materials);
            bom.Materials.Clear();

            foreach (var m in request.Materials)
            {
                var matCode = m.MaterialCode.Trim().ToLower();
                var rawMaterial = await _dbContext.RawMaterials
                    .FirstOrDefaultAsync(x => !x.IsDeleted && x.MaterialCode.ToLower() == matCode, cancellationToken);

                var matId = rawMaterial?.Id ?? m.MaterialId;
                if (matId == 0 && rawMaterial == null)
                {
                    throw new InvalidOperationException($"Raw material with code '{m.MaterialCode}' not found.");
                }

                bom.Materials.Add(new BomMaterialLine
                {
                    MaterialId = matId,
                    MaterialCode = m.MaterialCode.Trim(),
                    MaterialName = m.MaterialName.Trim(),
                    Quantity = m.Quantity,
                    Uom = string.IsNullOrWhiteSpace(m.Uom) ? "Nos" : m.Uom.Trim(),
                    WastagePercent = m.WastagePercent,
                    WarehouseId = m.WarehouseId == 0 ? 1 : m.WarehouseId,
                    WarehouseName = string.IsNullOrWhiteSpace(m.WarehouseName) ? "Main RM Store" : m.WarehouseName.Trim()
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToBomDto(bom);
        }

        public async Task<bool> DeleteBomAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var bom = await _dbContext.BillOfMaterials
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (bom is null) return false;

            if (bom.Status != BomStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft BOMs can be deleted");
            }

            var inUse = await _dbContext.ProductionPlans.AnyAsync(x => !x.IsDeleted && x.BomId == id, cancellationToken) ||
                        await _dbContext.WorkOrders.AnyAsync(x => !x.IsDeleted && x.BomId == id, cancellationToken);

            if (inUse)
            {
                throw new InvalidOperationException("BOM is referenced by plans or work orders");
            }

            bom.IsDeleted = true;
            bom.UpdatedBy = actingUser;
            bom.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<BomDto?> DuplicateBomAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var prev = await _dbContext.BillOfMaterials
                .Include(x => x.Materials)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (prev is null) return null;

            var bomNumber = await GenerateDocNumberAsync("BOM", cancellationToken);

            var bom = new BillOfMaterials
            {
                BomNumber = bomNumber,
                Version = "1.0",
                ProductId = prev.ProductId,
                ProductCode = prev.ProductCode,
                ProductName = prev.ProductName,
                Revision = prev.Revision + "-Copy",
                Status = BomStatus.Draft,
                EffectiveDate = prev.EffectiveDate,
                ExpiryDate = prev.ExpiryDate,
                ProcessNotes = prev.ProcessNotes,
                Notes = prev.Notes,
                CreatedBy = actingUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = actingUser,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var m in prev.Materials)
            {
                bom.Materials.Add(new BomMaterialLine
                {
                    MaterialId = m.MaterialId,
                    MaterialCode = m.MaterialCode,
                    MaterialName = m.MaterialName,
                    Quantity = m.Quantity,
                    Uom = m.Uom,
                    WastagePercent = m.WastagePercent,
                    WarehouseId = m.WarehouseId,
                    WarehouseName = m.WarehouseName
                });
            }

            _dbContext.BillOfMaterials.Add(bom);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToBomDto(bom);
        }

        public async Task<BomDto?> ApproveBomAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionBomInternalAsync(id, new[] { BomStatus.Draft }, BomStatus.Approved, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<BomDto?> ActivateBomAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionBomInternalAsync(id, new[] { BomStatus.Approved }, BomStatus.Active, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<BomDto?> ArchiveBomAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionBomInternalAsync(id, new[] { BomStatus.Active, BomStatus.Approved }, BomStatus.Archived, payload?.Remarks, actingUser, cancellationToken);
        }

        private async Task<BomDto?> TransitionBomInternalAsync(int id, BomStatus[] allowedFrom, BomStatus targetStatus, string? remarks, string actingUser, CancellationToken cancellationToken)
        {
            var bom = await _dbContext.BillOfMaterials
                .Include(x => x.Materials)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (bom is null) return null;

            if (!allowedFrom.Contains(bom.Status))
            {
                throw new InvalidOperationException($"Invalid BOM transition: {bom.Status} → {targetStatus}");
            }

            bom.Status = targetStatus;
            bom.UpdatedBy = actingUser;
            bom.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                bom.Notes = string.IsNullOrWhiteSpace(bom.Notes) ? remarks : $"{bom.Notes}\n[{actingUser} at {DateTime.UtcNow:yyyy-MM-dd}]: {remarks}";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToBomDto(bom);
        }

        public async Task<BomDashboardDto> GetBomDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.BillOfMaterials.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            return new BomDashboardDto
            {
                TotalBoms = list.Count,
                ActiveBoms = list.Count(x => x.Status == BomStatus.Active),
                DraftBoms = list.Count(x => x.Status == BomStatus.Draft),
                ArchivedBoms = list.Count(x => x.Status == BomStatus.Archived)
            };
        }


        // ── PRODUCTION PLANNING ──

        public async Task<List<PlanListItemDto>> GetPlansAsync(string? search, string? status, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.ProductionPlans.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.PlanNumber.ToLower().Contains(s) ||
                                 x.ProductCode.ToLower().Contains(s) ||
                                 x.ProductName.ToLower().Contains(s) ||
                                 x.Planner.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<PlanStatus>(status, true, out var statusEnum))
                {
                    q = q.Where(x => x.Status == statusEnum);
                }
            }

            var items = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
            return items.Select(MapToPlanListItemDto).ToList();
        }

        public async Task<PlanDto?> GetPlanByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var plan = await _dbContext.ProductionPlans
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return plan is null ? null : MapToPlanDto(plan);
        }

        public async Task<PlanDto> CreatePlanAsync(PlanCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var bom = await _dbContext.BillOfMaterials
                .FirstOrDefaultAsync(x => x.Id == request.BomId && !x.IsDeleted, cancellationToken);

            if (bom is null)
            {
                throw new InvalidOperationException("Plan must reference a valid BOM");
            }

            if (bom.Status != BomStatus.Approved && bom.Status != BomStatus.Active)
            {
                throw new InvalidOperationException("Plan must reference an Approved or Active BOM");
            }

            var expectedStart = DateOnly.Parse(request.ExpectedStart);
            var expectedFinish = DateOnly.Parse(request.ExpectedFinish);

            if (expectedFinish < expectedStart)
            {
                throw new InvalidOperationException("Expected finish must be on or after expected start");
            }

            if (request.PlannedQuantity <= 0 || request.RequiredQuantity <= 0)
            {
                throw new InvalidOperationException("Quantities must be greater than zero");
            }

            // Resolve Product ID
            var product = await _dbContext.FinishedGoods
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProductCode.ToLower() == request.ProductCode.Trim().ToLower(), cancellationToken);

            var productId = product?.Id ?? request.ProductId;
            if (productId == 0 && product == null)
            {
                throw new InvalidOperationException($"Product with code '{request.ProductCode}' not found.");
            }

            if (bom.ProductId != productId)
            {
                throw new InvalidOperationException("BOM does not belong to the selected product.");
            }

            var planNumber = await GenerateDocNumberAsync("PP", cancellationToken);

            var plan = new ProductionPlan
            {
                PlanNumber = planNumber,
                PlanningPeriod = request.PlanningPeriod.Trim(),
                ProductId = productId,
                ProductCode = request.ProductCode.Trim(),
                ProductName = request.ProductName.Trim(),
                RequiredQuantity = request.RequiredQuantity,
                PlannedQuantity = request.PlannedQuantity,
                BomId = bom.Id,
                BomNumber = bom.BomNumber,
                WarehouseId = request.WarehouseId == 0 ? 1 : request.WarehouseId,
                WarehouseName = string.IsNullOrWhiteSpace(request.WarehouseName) ? "Main Store" : request.WarehouseName.Trim(),
                Priority = Enum.TryParse<Priority>(request.Priority, true, out var p) ? p : Priority.Medium,
                Planner = request.Planner.Trim(),
                ExpectedStart = expectedStart,
                ExpectedFinish = expectedFinish,
                Status = PlanStatus.Draft,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedBy = actingUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = actingUser,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.ProductionPlans.Add(plan);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToPlanDto(plan);
        }

        public async Task<PlanDto?> UpdatePlanAsync(int id, PlanUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var plan = await _dbContext.ProductionPlans
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (plan is null) return null;

            if (plan.Status != PlanStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft plans can be edited");
            }

            var bom = await _dbContext.BillOfMaterials
                .FirstOrDefaultAsync(x => x.Id == request.BomId && !x.IsDeleted, cancellationToken);

            if (bom is null)
            {
                throw new InvalidOperationException("Plan must reference a valid BOM");
            }

            if (bom.Status != BomStatus.Approved && bom.Status != BomStatus.Active)
            {
                throw new InvalidOperationException("Plan must reference an Approved or Active BOM");
            }

            var expectedStart = DateOnly.Parse(request.ExpectedStart);
            var expectedFinish = DateOnly.Parse(request.ExpectedFinish);

            if (expectedFinish < expectedStart)
            {
                throw new InvalidOperationException("Expected finish must be on or after expected start");
            }

            if (request.PlannedQuantity <= 0 || request.RequiredQuantity <= 0)
            {
                throw new InvalidOperationException("Quantities must be greater than zero");
            }

            var product = await _dbContext.FinishedGoods
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProductCode.ToLower() == request.ProductCode.Trim().ToLower(), cancellationToken);

            var productId = product?.Id ?? request.ProductId;
            if (productId == 0 && product == null)
            {
                throw new InvalidOperationException($"Product with code '{request.ProductCode}' not found.");
            }

            if (bom.ProductId != productId)
            {
                throw new InvalidOperationException("BOM does not belong to the selected product.");
            }

            plan.PlanningPeriod = request.PlanningPeriod.Trim();
            plan.ProductId = productId;
            plan.ProductCode = request.ProductCode.Trim();
            plan.ProductName = request.ProductName.Trim();
            plan.RequiredQuantity = request.RequiredQuantity;
            plan.PlannedQuantity = request.PlannedQuantity;
            plan.BomId = bom.Id;
            plan.BomNumber = bom.BomNumber;
            plan.WarehouseId = request.WarehouseId == 0 ? 1 : request.WarehouseId;
            plan.WarehouseName = string.IsNullOrWhiteSpace(request.WarehouseName) ? "Main Store" : request.WarehouseName.Trim();
            plan.Priority = Enum.TryParse<Priority>(request.Priority, true, out var p) ? p : Priority.Medium;
            plan.Planner = request.Planner.Trim();
            plan.ExpectedStart = expectedStart;
            plan.ExpectedFinish = expectedFinish;
            plan.Notes = request.Notes?.Trim() ?? string.Empty;
            plan.UpdatedBy = actingUser;
            plan.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToPlanDto(plan);
        }

        public async Task<bool> DeletePlanAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var plan = await _dbContext.ProductionPlans
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (plan is null) return false;

            if (plan.Status != PlanStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft plans can be deleted");
            }

            var linked = await _dbContext.WorkOrders.AnyAsync(x => !x.IsDeleted && x.PlanId == id, cancellationToken);
            if (linked)
            {
                throw new InvalidOperationException("Plan has linked work orders");
            }

            plan.IsDeleted = true;
            plan.UpdatedBy = actingUser;
            plan.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<PlanDto?> ApprovePlanAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionPlanInternalAsync(id, new[] { PlanStatus.Draft }, PlanStatus.Approved, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<PlanDto?> ReleasePlanAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionPlanInternalAsync(id, new[] { PlanStatus.Approved }, PlanStatus.Released, payload?.Remarks, actingUser, cancellationToken);
        }

        private async Task<PlanDto?> TransitionPlanInternalAsync(int id, PlanStatus[] allowedFrom, PlanStatus targetStatus, string? remarks, string actingUser, CancellationToken cancellationToken)
        {
            var plan = await _dbContext.ProductionPlans
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (plan is null) return null;

            if (!allowedFrom.Contains(plan.Status))
            {
                throw new InvalidOperationException($"Invalid plan transition: {plan.Status} → {targetStatus}");
            }

            plan.Status = targetStatus;
            plan.UpdatedBy = actingUser;
            plan.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                plan.Notes = string.IsNullOrWhiteSpace(plan.Notes) ? remarks : $"{plan.Notes}\n[{actingUser} at {DateTime.UtcNow:yyyy-MM-dd}]: {remarks}";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToPlanDto(plan);
        }

        public async Task<List<WorkOrderDto>> GenerateWorkOrdersAsync(int planId, string actingUser, CancellationToken cancellationToken = default)
        {
            var plan = await _dbContext.ProductionPlans
                .FirstOrDefaultAsync(x => x.Id == planId && !x.IsDeleted, cancellationToken);

            if (plan is null)
            {
                throw new InvalidOperationException("Plan not found");
            }

            if (plan.Status != PlanStatus.Released)
            {
                throw new InvalidOperationException($"Cannot generate work orders from plan in status {plan.Status}");
            }

            var existing = await _dbContext.WorkOrders.Where(x => !x.IsDeleted && x.PlanId == planId).ToListAsync(cancellationToken);
            if (existing.Count > 0)
            {
                throw new InvalidOperationException($"Work orders already generated for {plan.PlanNumber} ({existing.Count} existing).");
            }

            var quantities = new List<decimal>();
            if (plan.PlannedQuantity <= 10m)
            {
                quantities.Add(plan.PlannedQuantity);
            }
            else
            {
                var first = Math.Ceiling(plan.PlannedQuantity / 2m);
                quantities.Add(first);
                quantities.Add(plan.PlannedQuantity - first);
            }

            var created = new List<WorkOrder>();
            for (int i = 0; i < quantities.Count; i++)
            {
                var qty = quantities[i];
                if (qty <= 0) continue;

                var woNumber = await GenerateDocNumberAsync("WO", cancellationToken);
                var wo = new WorkOrder
                {
                    WorkOrderNumber = woNumber,
                    PlanId = plan.Id,
                    PlanNumber = plan.PlanNumber,
                    BomId = plan.BomId,
                    BomNumber = plan.BomNumber,
                    ProductId = plan.ProductId,
                    ProductCode = plan.ProductCode,
                    ProductName = plan.ProductName,
                    PlannedQuantity = qty,
                    ProducedQuantity = 0m,
                    PendingQuantity = qty,
                    Priority = plan.Priority,
                    Supervisor = plan.Planner,
                    MachineId = null,
                    MachineCode = null,
                    MachineName = null,
                    AssignedTeam = i == 0 ? "Team Alpha" : "Team Beta",
                    StartDate = plan.ExpectedStart,
                    DueDate = plan.ExpectedFinish,
                    Status = WorkOrderStatus.Draft,
                    Notes = $"Auto-generated from {plan.PlanNumber}",
                    CreatedBy = actingUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = actingUser,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.WorkOrders.Add(wo);
                created.Add(wo);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return created.Select(MapToWorkOrderDto).ToList();
        }

        public async Task<PlanDashboardDto> GetPlanDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.ProductionPlans.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            return new PlanDashboardDto
            {
                TotalPlans = list.Count,
                PendingPlans = list.Count(x => x.Status == PlanStatus.Draft || x.Status == PlanStatus.Approved),
                ReleasedPlans = list.Count(x => x.Status == PlanStatus.Released),
                CompletedPlans = list.Count(x => x.Status == PlanStatus.Completed)
            };
        }


        // ── WORK ORDERS ──

        public async Task<List<WorkOrderListItemDto>> GetWorkOrdersAsync(string? search, string? status, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.WorkOrders.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.WorkOrderNumber.ToLower().Contains(s) ||
                                 x.PlanNumber.ToLower().Contains(s) ||
                                 x.ProductCode.ToLower().Contains(s) ||
                                 x.ProductName.ToLower().Contains(s) ||
                                 x.Supervisor.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("In Progress", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(x => x.Status == WorkOrderStatus.InProgress);
                }
                else if (Enum.TryParse<WorkOrderStatus>(status.Replace(" ", ""), true, out var statusEnum))
                {
                    q = q.Where(x => x.Status == statusEnum);
                }
            }

            var items = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
            return items.Select(MapToWorkOrderListItemDto).ToList();
        }

        public async Task<WorkOrderDto?> GetWorkOrderByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var wo = await _dbContext.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return wo is null ? null : MapToWorkOrderDto(wo);
        }

        public async Task<WorkOrderDto> CreateWorkOrderAsync(WorkOrderCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var plan = await _dbContext.ProductionPlans.FirstOrDefaultAsync(x => x.Id == request.PlanId && !x.IsDeleted, cancellationToken);
            if (plan is null)
            {
                throw new InvalidOperationException("Plan not found");
            }

            var bom = await _dbContext.BillOfMaterials.FirstOrDefaultAsync(x => x.Id == request.BomId && !x.IsDeleted, cancellationToken);
            if (bom is null)
            {
                throw new InvalidOperationException("BOM not found");
            }

            var product = await _dbContext.FinishedGoods
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProductCode.ToLower() == request.ProductCode.Trim().ToLower(), cancellationToken);

            var productId = product?.Id ?? request.ProductId;
            if (productId == 0 && product == null)
            {
                throw new InvalidOperationException($"Product with code '{request.ProductCode}' not found.");
            }

            var startDate = DateOnly.Parse(request.StartDate);
            var dueDate = DateOnly.Parse(request.DueDate);

            if (dueDate < startDate)
            {
                throw new InvalidOperationException("Due date must be on or after start date");
            }

            if (request.PlannedQuantity <= 0)
            {
                throw new InvalidOperationException("Planned quantity must be greater than zero");
            }

            // Resolve Machine
            Machine? machine = null;
            if (request.MachineId.HasValue)
            {
                machine = await _dbContext.Machines.FirstOrDefaultAsync(x => x.Id == request.MachineId && !x.IsDeleted, cancellationToken);
                if (machine is null)
                {
                    throw new InvalidOperationException("Selected Machine not found.");
                }
            }

            var woNumber = await GenerateDocNumberAsync("WO", cancellationToken);

            var wo = new WorkOrder
            {
                WorkOrderNumber = woNumber,
                PlanId = plan.Id,
                PlanNumber = plan.PlanNumber,
                BomId = bom.Id,
                BomNumber = bom.BomNumber,
                ProductId = productId,
                ProductCode = request.ProductCode.Trim(),
                ProductName = request.ProductName.Trim(),
                PlannedQuantity = request.PlannedQuantity,
                ProducedQuantity = 0m,
                PendingQuantity = request.PlannedQuantity,
                Priority = Enum.TryParse<Priority>(request.Priority, true, out var p) ? p : Priority.Medium,
                Supervisor = request.Supervisor.Trim(),
                MachineId = machine?.Id,
                MachineCode = machine?.MachineCode,
                MachineName = machine?.MachineName,
                AssignedTeam = request.AssignedTeam.Trim(),
                StartDate = startDate,
                DueDate = dueDate,
                Status = WorkOrderStatus.Draft,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedBy = actingUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = actingUser,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.WorkOrders.Add(wo);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToWorkOrderDto(wo);
        }

        public async Task<WorkOrderDto?> UpdateWorkOrderAsync(int id, WorkOrderUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var wo = await _dbContext.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (wo is null) return null;

            if (wo.Status != WorkOrderStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft work orders can be edited");
            }

            var plan = await _dbContext.ProductionPlans.FirstOrDefaultAsync(x => x.Id == request.PlanId && !x.IsDeleted, cancellationToken);
            if (plan is null)
            {
                throw new InvalidOperationException("Plan not found");
            }

            var bom = await _dbContext.BillOfMaterials.FirstOrDefaultAsync(x => x.Id == request.BomId && !x.IsDeleted, cancellationToken);
            if (bom is null)
            {
                throw new InvalidOperationException("BOM not found");
            }

            var product = await _dbContext.FinishedGoods
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProductCode.ToLower() == request.ProductCode.Trim().ToLower(), cancellationToken);

            var productId = product?.Id ?? request.ProductId;
            if (productId == 0 && product == null)
            {
                throw new InvalidOperationException($"Product with code '{request.ProductCode}' not found.");
            }

            var startDate = DateOnly.Parse(request.StartDate);
            var dueDate = DateOnly.Parse(request.DueDate);

            if (dueDate < startDate)
            {
                throw new InvalidOperationException("Due date must be on or after start date");
            }

            if (request.PlannedQuantity <= 0)
            {
                throw new InvalidOperationException("Planned quantity must be greater than zero");
            }

            // Resolve Machine
            Machine? machine = null;
            if (request.MachineId.HasValue)
            {
                machine = await _dbContext.Machines.FirstOrDefaultAsync(x => x.Id == request.MachineId && !x.IsDeleted, cancellationToken);
                if (machine is null)
                {
                    throw new InvalidOperationException("Selected Machine not found.");
                }
            }

            wo.PlanId = plan.Id;
            wo.PlanNumber = plan.PlanNumber;
            wo.BomId = bom.Id;
            wo.BomNumber = bom.BomNumber;
            wo.ProductId = productId;
            wo.ProductCode = request.ProductCode.Trim();
            wo.ProductName = request.ProductName.Trim();
            wo.PlannedQuantity = request.PlannedQuantity;
            wo.PendingQuantity = Math.Max(0m, request.PlannedQuantity - wo.ProducedQuantity);
            wo.Priority = Enum.TryParse<Priority>(request.Priority, true, out var p) ? p : Priority.Medium;
            wo.Supervisor = request.Supervisor.Trim();
            wo.MachineId = machine?.Id;
            wo.MachineCode = machine?.MachineCode;
            wo.MachineName = machine?.MachineName;
            wo.AssignedTeam = request.AssignedTeam.Trim();
            wo.StartDate = startDate;
            wo.DueDate = dueDate;
            wo.Notes = request.Notes?.Trim() ?? string.Empty;
            wo.UpdatedBy = actingUser;
            wo.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToWorkOrderDto(wo);
        }

        public async Task<bool> DeleteWorkOrderAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var wo = await _dbContext.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (wo is null) return false;

            if (wo.Status != WorkOrderStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft work orders can be deleted");
            }

            wo.IsDeleted = true;
            wo.UpdatedBy = actingUser;
            wo.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<WorkOrderDto?> StartWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionWorkOrderInternalAsync(id, new[] { WorkOrderStatus.Released, WorkOrderStatus.Paused }, WorkOrderStatus.InProgress, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<WorkOrderDto?> ReleaseWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionWorkOrderInternalAsync(id, new[] { WorkOrderStatus.Draft }, WorkOrderStatus.Released, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<WorkOrderDto?> PauseWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionWorkOrderInternalAsync(id, new[] { WorkOrderStatus.InProgress }, WorkOrderStatus.Paused, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<WorkOrderDto?> ResumeWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionWorkOrderInternalAsync(id, new[] { WorkOrderStatus.Paused }, WorkOrderStatus.InProgress, payload?.Remarks, actingUser, cancellationToken);
        }

        public async Task<WorkOrderDto?> CompleteWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload?.Remarks))
            {
                throw new InvalidOperationException("Completion remarks are required");
            }
            return await TransitionWorkOrderInternalAsync(id, new[] { WorkOrderStatus.InProgress, WorkOrderStatus.Paused }, WorkOrderStatus.Completed, payload.Remarks, actingUser, cancellationToken);
        }

        public async Task<WorkOrderDto?> CloseWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            return await TransitionWorkOrderInternalAsync(id, new[] { WorkOrderStatus.Completed }, WorkOrderStatus.Closed, payload?.Remarks, actingUser, cancellationToken);
        }

        private async Task<WorkOrderDto?> TransitionWorkOrderInternalAsync(int id, WorkOrderStatus[] allowedFrom, WorkOrderStatus targetStatus, string? remarks, string actingUser, CancellationToken cancellationToken)
        {
            var wo = await _dbContext.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (wo is null) return null;

            if (!allowedFrom.Contains(wo.Status))
            {
                throw new InvalidOperationException($"Invalid work order transition: {wo.Status} → {targetStatus}");
            }

            wo.Status = targetStatus;
            wo.UpdatedBy = actingUser;
            wo.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                wo.Notes = string.IsNullOrWhiteSpace(wo.Notes) ? remarks : $"{wo.Notes}\n[{actingUser} at {DateTime.UtcNow:yyyy-MM-dd}]: {remarks}";
            }

            // AUTO-COMPLETE PARENT PLAN LOGIC:
            if (targetStatus == WorkOrderStatus.Completed || targetStatus == WorkOrderStatus.Closed)
            {
                var allPlanWos = await _dbContext.WorkOrders
                    .Where(x => !x.IsDeleted && x.PlanId == wo.PlanId)
                    .ToListAsync(cancellationToken);

                // If all other work orders are completed/closed, we complete the parent plan
                if (allPlanWos.All(x => x.Id == wo.Id || x.Status == WorkOrderStatus.Completed || x.Status == WorkOrderStatus.Closed))
                {
                    var plan = await _dbContext.ProductionPlans
                        .FirstOrDefaultAsync(x => x.Id == wo.PlanId && !x.IsDeleted, cancellationToken);

                    if (plan != null && plan.Status == PlanStatus.Released)
                    {
                        plan.Status = PlanStatus.Completed;
                        plan.UpdatedBy = "system";
                        plan.UpdatedAt = DateTime.UtcNow;
                        plan.Notes = string.IsNullOrWhiteSpace(plan.Notes) 
                            ? "Plan completed automatically when all child work orders completed." 
                            : $"{plan.Notes}\n[system at {DateTime.UtcNow:yyyy-MM-dd}]: Plan completed automatically when all child work orders completed.";
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToWorkOrderDto(wo);
        }

        public async Task<WorkOrderDashboardDto> GetWorkOrderDashboardAsync(CancellationToken cancellationToken = default)
        {
            var wos = await _dbContext.WorkOrders.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            return new WorkOrderDashboardDto
            {
                OpenWorkOrders = wos.Count(w => w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Closed),
                Running = wos.Count(w => w.Status == WorkOrderStatus.InProgress),
                Completed = wos.Count(w => w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Closed),
                Delayed = wos.Count(w => w.DueDate < today && w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Closed)
            };
        }


        // ── MAPPER HELPERS ──

        private static BomListItemDto MapToBomListItemDto(BillOfMaterials b)
        {
            return new BomListItemDto
            {
                Id = b.Id,
                BomNumber = b.BomNumber,
                Version = b.Version,
                ProductCode = b.ProductCode,
                ProductName = b.ProductName,
                Revision = b.Revision,
                Status = b.Status.ToString(),
                EffectiveDate = b.EffectiveDate.ToString("yyyy-MM-dd"),
                MaterialCount = b.Materials.Count,
                UpdatedAt = b.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        private static BomDto MapToBomDto(BillOfMaterials b)
        {
            var timeline = new List<ProductionTimelineEventDto>
            {
                new()
                {
                    Id = $"bom-tl-created-{b.Id}",
                    Date = b.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = b.CreatedBy,
                    Action = "BOM created",
                    ToStatus = BomStatus.Draft.ToString()
                }
            };

            if (b.Status != BomStatus.Draft)
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"bom-tl-status-{b.Id}",
                    Date = b.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = b.UpdatedBy,
                    Action = $"Status → {b.Status}",
                    ToStatus = b.Status.ToString()
                });
            }

            return new BomDto
            {
                Id = b.Id,
                BomNumber = b.BomNumber,
                Version = b.Version,
                ProductId = b.ProductId,
                ProductCode = b.ProductCode,
                ProductName = b.ProductName,
                Revision = b.Revision,
                Status = b.Status.ToString(),
                EffectiveDate = b.EffectiveDate.ToString("yyyy-MM-dd"),
                ExpiryDate = b.ExpiryDate?.ToString("yyyy-MM-dd"),
                ProcessNotes = b.ProcessNotes,
                Notes = b.Notes,
                Attachments = new List<ProductionAttachmentDto>(),
                Timeline = timeline,
                CreatedBy = b.CreatedBy,
                CreatedAt = b.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedBy = b.UpdatedBy,
                UpdatedAt = b.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Materials = b.Materials.Select(m => new BomMaterialLineDto
                {
                    Id = $"bm-{m.Id}",
                    MaterialId = m.MaterialId,
                    MaterialCode = m.MaterialCode,
                    MaterialName = m.MaterialName,
                    Quantity = m.Quantity,
                    Uom = m.Uom,
                    WastagePercent = m.WastagePercent,
                    WarehouseId = m.WarehouseId,
                    WarehouseName = m.WarehouseName
                }).ToList()
            };
        }

        private static PlanListItemDto MapToPlanListItemDto(ProductionPlan p)
        {
            return new PlanListItemDto
            {
                Id = p.Id,
                PlanNumber = p.PlanNumber,
                PlanningPeriod = p.PlanningPeriod,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                RequiredQuantity = p.RequiredQuantity,
                PlannedQuantity = p.PlannedQuantity,
                BomNumber = p.BomNumber,
                WarehouseName = p.WarehouseName,
                Priority = p.Priority.ToString(),
                Planner = p.Planner,
                ExpectedStart = p.ExpectedStart.ToString("yyyy-MM-dd"),
                ExpectedFinish = p.ExpectedFinish.ToString("yyyy-MM-dd"),
                Status = p.Status.ToString()
            };
        }

        private static PlanDto MapToPlanDto(ProductionPlan p)
        {
            var timeline = new List<ProductionTimelineEventDto>
            {
                new()
                {
                    Id = $"plan-tl-created-{p.Id}",
                    Date = p.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = p.CreatedBy,
                    Action = "Plan created",
                    ToStatus = PlanStatus.Draft.ToString()
                }
            };

            if (p.Status != PlanStatus.Draft)
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"plan-tl-status-{p.Id}",
                    Date = p.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = p.UpdatedBy,
                    Action = $"Status → {p.Status}",
                    ToStatus = p.Status.ToString()
                });
            }

            return new PlanDto
            {
                Id = p.Id,
                PlanNumber = p.PlanNumber,
                PlanningPeriod = p.PlanningPeriod,
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                RequiredQuantity = p.RequiredQuantity,
                PlannedQuantity = p.PlannedQuantity,
                BomId = p.BomId,
                BomNumber = p.BomNumber,
                WarehouseId = p.WarehouseId,
                WarehouseName = p.WarehouseName,
                Priority = p.Priority.ToString(),
                Planner = p.Planner,
                ExpectedStart = p.ExpectedStart.ToString("yyyy-MM-dd"),
                ExpectedFinish = p.ExpectedFinish.ToString("yyyy-MM-dd"),
                Status = p.Status.ToString(),
                Notes = p.Notes,
                Attachments = new List<ProductionAttachmentDto>(),
                Timeline = timeline,
                CreatedBy = p.CreatedBy,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedBy = p.UpdatedBy,
                UpdatedAt = p.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        private static WorkOrderListItemDto MapToWorkOrderListItemDto(WorkOrder w)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return new WorkOrderListItemDto
            {
                Id = w.Id,
                WorkOrderNumber = w.WorkOrderNumber,
                PlanNumber = w.PlanNumber,
                BomNumber = w.BomNumber,
                ProductCode = w.ProductCode,
                ProductName = w.ProductName,
                PlannedQuantity = w.PlannedQuantity,
                ProducedQuantity = w.ProducedQuantity,
                PendingQuantity = w.PendingQuantity,
                Priority = w.Priority.ToString(),
                Supervisor = w.Supervisor,
                MachineName = w.MachineName,
                StartDate = w.StartDate.ToString("yyyy-MM-dd"),
                DueDate = w.DueDate.ToString("yyyy-MM-dd"),
                Status = ConvertWorkOrderStatusString(w.Status),
                IsDelayed = w.DueDate < today && w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Closed
            };
        }

        private static WorkOrderDto MapToWorkOrderDto(WorkOrder w)
        {
            var timeline = new List<ProductionTimelineEventDto>
            {
                new()
                {
                    Id = $"wo-tl-created-{w.Id}",
                    Date = w.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = w.CreatedBy,
                    Action = "Generated from plan",
                    ToStatus = WorkOrderStatus.Draft.ToString()
                }
            };

            if (w.Status != WorkOrderStatus.Draft)
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"wo-tl-status-{w.Id}",
                    Date = w.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = w.UpdatedBy,
                    Action = $"Status → {ConvertWorkOrderStatusString(w.Status)}",
                    ToStatus = ConvertWorkOrderStatusString(w.Status)
                });
            }

            return new WorkOrderDto
            {
                Id = w.Id,
                WorkOrderNumber = w.WorkOrderNumber,
                PlanId = w.PlanId,
                PlanNumber = w.PlanNumber,
                BomId = w.BomId,
                BomNumber = w.BomNumber,
                ProductId = w.ProductId,
                ProductCode = w.ProductCode,
                ProductName = w.ProductName,
                PlannedQuantity = w.PlannedQuantity,
                ProducedQuantity = w.ProducedQuantity,
                PendingQuantity = w.PendingQuantity,
                Priority = w.Priority.ToString(),
                Supervisor = w.Supervisor,
                MachineId = w.MachineId,
                MachineCode = w.MachineCode,
                MachineName = w.MachineName,
                AssignedTeam = w.AssignedTeam,
                StartDate = w.StartDate.ToString("yyyy-MM-dd"),
                DueDate = w.DueDate.ToString("yyyy-MM-dd"),
                Status = ConvertWorkOrderStatusString(w.Status),
                Notes = w.Notes,
                Attachments = new List<ProductionAttachmentDto>(),
                Timeline = timeline,
                CreatedBy = w.CreatedBy,
                CreatedAt = w.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedBy = w.UpdatedBy,
                UpdatedAt = w.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        private static string ConvertWorkOrderStatusString(WorkOrderStatus status)
        {
            return status == WorkOrderStatus.InProgress ? "In Progress" : status.ToString();
        }
    }
}
