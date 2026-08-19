using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Production;
using ERP.Application.Production.Dtos;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Domain.Production;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using StatusActionRequestDto = ERP.Application.Production.Dtos.StatusActionRequestDto;

namespace ERP.Infrastructure.Production
{
    public class ProductionService : IProductionService
    {
        private readonly ERPDbContext _dbContext;
        private readonly IStoreInventoryService _inventoryService;

        public ProductionService(ERPDbContext dbContext, IStoreInventoryService inventoryService)
        {
            _dbContext = dbContext;
            _inventoryService = inventoryService;
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

        // ── AUTO-SEEDING FOR DEMONSTRATION & COMPATIBILITY ──

        private async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
        {
            // Seed Finished Goods if empty
            if (!await _dbContext.FinishedGoods.AnyAsync(cancellationToken))
            {
                var fg1 = new FinishedGood
                {
                    ProductCode = "FG-HSG-100",
                    ProductName = "Precision Housing Assy",
                    AvailableQuantity = 100,
                    WarehouseId = 1,
                    WarehouseName = "Main Store",
                    Unit = "Nos"
                };
                var fg2 = new FinishedGood
                {
                    ProductCode = "FG-SHAFT-55",
                    ProductName = "Drive Shaft 55mm",
                    AvailableQuantity = 200,
                    WarehouseId = 1,
                    WarehouseName = "Main Store",
                    Unit = "Nos"
                };
                _dbContext.FinishedGoods.AddRange(fg1, fg2);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var fgs = await _dbContext.FinishedGoods.ToListAsync(cancellationToken);
            var fgHsg = fgs.FirstOrDefault(x => x.ProductCode == "FG-HSG-100") ?? fgs[0];
            var fgShaft = fgs.FirstOrDefault(x => x.ProductCode == "FG-SHAFT-55") ?? fgs[0];

            // Seed Warehouse if empty
            if (!await _dbContext.Warehouses.AnyAsync(cancellationToken))
            {
                var wh = new Warehouse
                {
                    Code = "WH-001",
                    Name = "Main Store",
                    Location = "Zone A",
                    Capacity = 10000
                };
                _dbContext.Warehouses.Add(wh);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            var warehouse = await _dbContext.Warehouses.FirstAsync(cancellationToken);

            // Seed BOM if empty
            if (!await _dbContext.BillOfMaterials.AnyAsync(cancellationToken))
            {
                var bom1 = new BillOfMaterials
                {
                    BomNumber = "BOM-2026-001",
                    Version = "1.0",
                    ProductId = fgHsg.Id,
                    ProductCode = fgHsg.ProductCode,
                    ProductName = fgHsg.ProductName,
                    Revision = "Rev 0",
                    Status = BomStatus.Active,
                    EffectiveDate = DateOnly.Parse("2026-01-01")
                };
                var bom2 = new BillOfMaterials
                {
                    BomNumber = "BOM-2026-002",
                    Version = "1.0",
                    ProductId = fgShaft.Id,
                    ProductCode = fgShaft.ProductCode,
                    ProductName = fgShaft.ProductName,
                    Revision = "Rev 0",
                    Status = BomStatus.Active,
                    EffectiveDate = DateOnly.Parse("2026-01-01")
                };
                _dbContext.BillOfMaterials.AddRange(bom1, bom2);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            var boms = await _dbContext.BillOfMaterials.ToListAsync(cancellationToken);
            var bom1Obj = boms.FirstOrDefault(x => x.BomNumber == "BOM-2026-001") ?? boms[0];
            var bom2Obj = boms.FirstOrDefault(x => x.BomNumber == "BOM-2026-002") ?? boms[0];

            // Seed plans if empty
            if (!await _dbContext.ProductionPlans.AnyAsync(cancellationToken))
            {
                var plan1 = new ProductionPlan
                {
                    PlanNumber = "PP-2026-071",
                    PlanningPeriod = "Jul 2026",
                    ProductId = fgHsg.Id,
                    ProductCode = fgHsg.ProductCode,
                    ProductName = fgHsg.ProductName,
                    RequiredQuantity = 48,
                    PlannedQuantity = 48,
                    BomId = bom1Obj.Id,
                    BomNumber = bom1Obj.BomNumber,
                    WarehouseId = warehouse.Id,
                    WarehouseName = warehouse.Name,
                    Priority = Priority.High,
                    Planner = "Priya Sharma",
                    ExpectedStart = DateOnly.Parse("2026-07-20"),
                    ExpectedFinish = DateOnly.Parse("2026-07-31"),
                    Status = PlanStatus.Released
                };
                var plan2 = new ProductionPlan
                {
                    PlanNumber = "PP-2026-072",
                    PlanningPeriod = "Jul 2026",
                    ProductId = fgShaft.Id,
                    ProductCode = fgShaft.ProductCode,
                    ProductName = fgShaft.ProductName,
                    RequiredQuantity = 90,
                    PlannedQuantity = 90,
                    BomId = bom2Obj.Id,
                    BomNumber = bom2Obj.BomNumber,
                    WarehouseId = warehouse.Id,
                    WarehouseName = warehouse.Name,
                    Priority = Priority.Medium,
                    Planner = "Neha Joshi",
                    ExpectedStart = DateOnly.Parse("2026-07-22"),
                    ExpectedFinish = DateOnly.Parse("2026-07-26"),
                    Status = PlanStatus.Draft
                };
                var plan3 = new ProductionPlan
                {
                    PlanNumber = "PP-2026-068",
                    PlanningPeriod = "Jun 2026",
                    ProductId = fgHsg.Id,
                    ProductCode = fgHsg.ProductCode,
                    ProductName = fgHsg.ProductName,
                    RequiredQuantity = 40,
                    PlannedQuantity = 40,
                    BomId = bom1Obj.Id,
                    BomNumber = bom1Obj.BomNumber,
                    WarehouseId = warehouse.Id,
                    WarehouseName = warehouse.Name,
                    Priority = Priority.Medium,
                    Planner = "Priya Sharma",
                    ExpectedStart = DateOnly.Parse("2026-06-10"),
                    ExpectedFinish = DateOnly.Parse("2026-06-28"),
                    Status = PlanStatus.Completed
                };
                _dbContext.ProductionPlans.AddRange(plan1, plan2, plan3);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            var plans = await _dbContext.ProductionPlans.ToListAsync(cancellationToken);
            var plan1Obj = plans.FirstOrDefault(x => x.PlanNumber == "PP-2026-071") ?? plans[0];
            var plan2Obj = plans.FirstOrDefault(x => x.PlanNumber == "PP-2026-072") ?? plans[0];
            var plan3Obj = plans.FirstOrDefault(x => x.PlanNumber == "PP-2026-068") ?? plans[0];

            // Seed machines if empty
            if (!await _dbContext.Machines.AnyAsync(cancellationToken))
            {
                var m1 = new Machine
                {
                    MachineCode = "MCH-CNC-01",
                    MachineName = "CNC Lathe Station 1",
                    Department = "Machining",
                    RunningHours = 148m,
                    IdleHours = 22m,
                    BreakdownHours = 6m,
                    UtilizationPercent = 84m,
                    MaintenanceDue = DateOnly.Parse("2026-08-15"),
                    Status = MachineStatus.Running,
                    Notes = "Primary pump casing machining.",
                    CreatedBy = "Admin",
                    UpdatedBy = "Operator Desk"
                };
                var m2 = new Machine
                {
                    MachineCode = "MCH-ASM-02",
                    MachineName = "Assembly Line 2",
                    Department = "Assembly",
                    RunningHours = 132m,
                    IdleHours = 40m,
                    BreakdownHours = 4m,
                    UtilizationPercent = 75m,
                    MaintenanceDue = DateOnly.Parse("2026-08-01"),
                    Status = MachineStatus.Running,
                    Notes = "Valve assembly line.",
                    CreatedBy = "Admin",
                    UpdatedBy = "Operator Desk"
                };
                var m3 = new Machine
                {
                    MachineCode = "MCH-WND-03",
                    MachineName = "Winding Machine 3",
                    Department = "Electrical",
                    RunningHours = 90m,
                    IdleHours = 70m,
                    BreakdownHours = 16m,
                    UtilizationPercent = 51m,
                    MaintenanceDue = DateOnly.Parse("2026-07-30"),
                    Status = MachineStatus.Idle,
                    Notes = "Awaiting motor plan release.",
                    CreatedBy = "Admin",
                    UpdatedBy = "Operator Desk"
                };
                var m4 = new Machine
                {
                    MachineCode = "MCH-TST-04",
                    MachineName = "Test Bench 4",
                    Department = "Quality",
                    RunningHours = 60m,
                    IdleHours = 20m,
                    BreakdownHours = 96m,
                    UtilizationPercent = 34m,
                    MaintenanceDue = DateOnly.Parse("2026-07-28"),
                    Status = MachineStatus.Breakdown,
                    Notes = "Hydraulic leak — maintenance ticket MT-442.",
                    CreatedBy = "Admin",
                    UpdatedBy = "Vikram Singh"
                };
                _dbContext.Machines.AddRange(m1, m2, m3, m4);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            var machines = await _dbContext.Machines.ToListAsync(cancellationToken);
            var m1Obj = machines.FirstOrDefault(x => x.MachineCode == "MCH-CNC-01") ?? machines[0];
            var m2Obj = machines.FirstOrDefault(x => x.MachineCode == "MCH-ASM-02") ?? machines[0];

            // Seed work orders if empty
            if (!await _dbContext.WorkOrders.AnyAsync(cancellationToken))
            {
                var wo1 = new WorkOrder
                {
                    WorkOrderNumber = "WO-2026-301",
                    PlanId = plan1Obj.Id,
                    PlanNumber = plan1Obj.PlanNumber,
                    BomId = bom1Obj.Id,
                    BomNumber = bom1Obj.BomNumber,
                    ProductId = fgHsg.Id,
                    ProductCode = fgHsg.ProductCode,
                    ProductName = fgHsg.ProductName,
                    PlannedQuantity = 24m,
                    ProducedQuantity = 10m,
                    PendingQuantity = 14m,
                    Priority = Priority.High,
                    Supervisor = "Vikram Singh",
                    MachineId = m1Obj.Id,
                    MachineCode = m1Obj.MachineCode,
                    MachineName = m1Obj.MachineName,
                    AssignedTeam = "Team Alpha",
                    StartDate = DateOnly.Parse("2026-07-20"),
                    DueDate = DateOnly.Parse("2026-07-28"),
                    Status = WorkOrderStatus.InProgress,
                    Notes = "First batch of July pump plan."
                };
                var wo2 = new WorkOrder
                {
                    WorkOrderNumber = "WO-2026-302",
                    PlanId = plan1Obj.Id,
                    PlanNumber = plan1Obj.PlanNumber,
                    BomId = bom1Obj.Id,
                    BomNumber = bom1Obj.BomNumber,
                    ProductId = fgHsg.Id,
                    ProductCode = fgHsg.ProductCode,
                    ProductName = fgHsg.ProductName,
                    PlannedQuantity = 24m,
                    ProducedQuantity = 0m,
                    PendingQuantity = 24m,
                    Priority = Priority.High,
                    Supervisor = "Vikram Singh",
                    MachineId = m1Obj.Id,
                    MachineCode = m1Obj.MachineCode,
                    MachineName = m1Obj.MachineName,
                    AssignedTeam = "Team Beta",
                    StartDate = DateOnly.Parse("2026-07-25"),
                    DueDate = DateOnly.Parse("2026-07-31"),
                    Status = WorkOrderStatus.Released,
                    Notes = "Second batch — starts after WO-301 mid-point."
                };
                var wo3 = new WorkOrder
                {
                    WorkOrderNumber = "WO-2026-290",
                    PlanId = plan3Obj.Id,
                    PlanNumber = plan3Obj.PlanNumber,
                    BomId = bom1Obj.Id,
                    BomNumber = bom1Obj.BomNumber,
                    ProductId = fgHsg.Id,
                    ProductCode = fgHsg.ProductCode,
                    ProductName = fgHsg.ProductName,
                    PlannedQuantity = 40m,
                    ProducedQuantity = 40m,
                    PendingQuantity = 0m,
                    Priority = Priority.Medium,
                    Supervisor = "Vikram Singh",
                    MachineId = m1Obj.Id,
                    MachineCode = m1Obj.MachineCode,
                    MachineName = m1Obj.MachineName,
                    AssignedTeam = "Team Alpha",
                    StartDate = DateOnly.Parse("2026-06-10"),
                    DueDate = DateOnly.Parse("2026-06-28"),
                    Status = WorkOrderStatus.Closed,
                    Notes = "June plan WO closed."
                };
                var wo4 = new WorkOrder
                {
                    WorkOrderNumber = "WO-2026-303",
                    PlanId = plan2Obj.Id,
                    PlanNumber = plan2Obj.PlanNumber,
                    BomId = bom2Obj.Id,
                    BomNumber = bom2Obj.BomNumber,
                    ProductId = fgShaft.Id,
                    ProductCode = fgShaft.ProductCode,
                    ProductName = fgShaft.ProductName,
                    PlannedQuantity = 90m,
                    ProducedQuantity = 0m,
                    PendingQuantity = 90m,
                    Priority = Priority.Medium,
                    Supervisor = "Anita Rao",
                    MachineId = m2Obj.Id,
                    MachineCode = m2Obj.MachineCode,
                    MachineName = m2Obj.MachineName,
                    AssignedTeam = "Team Gamma",
                    StartDate = DateOnly.Parse("2026-07-22"),
                    DueDate = DateOnly.Parse("2026-07-26"),
                    Status = WorkOrderStatus.Draft,
                    Notes = "Will release after plan release."
                };
                _dbContext.WorkOrders.AddRange(wo1, wo2, wo3, wo4);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            var wos = await _dbContext.WorkOrders.ToListAsync(cancellationToken);
            var wo1Obj = wos.FirstOrDefault(x => x.WorkOrderNumber == "WO-2026-301") ?? wos[0];
            var wo2Obj = wos.FirstOrDefault(x => x.WorkOrderNumber == "WO-2026-302") ?? wos[0];
            var wo3Obj = wos.FirstOrDefault(x => x.WorkOrderNumber == "WO-2026-290") ?? wos[0];

            // Seed schedules if empty
            if (!await _dbContext.ProductionSchedules.AnyAsync(cancellationToken))
            {
                var s1 = new ProductionSchedule
                {
                    ScheduleNumber = "SCH-2026-501",
                    WorkOrderId = wo1Obj.Id,
                    WorkOrderNumber = wo1Obj.WorkOrderNumber,
                    ProductName = wo1Obj.ProductName,
                    MachineId = m1Obj.Id,
                    MachineCode = m1Obj.MachineCode,
                    MachineName = m1Obj.MachineName,
                    Shift = Shift.A,
                    Operator = "Rahul Mehta",
                    StartTime = DateTime.Parse("2026-07-29T06:00:00Z").ToUniversalTime(),
                    EndTime = DateTime.Parse("2026-07-29T14:00:00Z").ToUniversalTime(),
                    Capacity = 8m,
                    UtilizationPercent = 88m,
                    DelayMinutes = 0,
                    Status = ScheduleStatus.Running,
                    Notes = "Today Shift A — casing machining."
                };
                var s2 = new ProductionSchedule
                {
                    ScheduleNumber = "SCH-2026-502",
                    WorkOrderId = wo1Obj.Id,
                    WorkOrderNumber = wo1Obj.WorkOrderNumber,
                    ProductName = wo1Obj.ProductName,
                    MachineId = m1Obj.Id,
                    MachineCode = m1Obj.MachineCode,
                    MachineName = m1Obj.MachineName,
                    Shift = Shift.B,
                    Operator = "Kavita Nair",
                    StartTime = DateTime.Parse("2026-07-29T14:00:00Z").ToUniversalTime(),
                    EndTime = DateTime.Parse("2026-07-29T22:00:00Z").ToUniversalTime(),
                    Capacity = 8m,
                    UtilizationPercent = 0m,
                    DelayMinutes = 0,
                    Status = ScheduleStatus.Scheduled,
                    Notes = "Upcoming Shift B."
                };
                var s3 = new ProductionSchedule
                {
                    ScheduleNumber = "SCH-2026-498",
                    WorkOrderId = wo3Obj.Id,
                    WorkOrderNumber = wo3Obj.WorkOrderNumber,
                    ProductName = wo3Obj.ProductName,
                    MachineId = m2Obj.Id,
                    MachineCode = m2Obj.MachineCode,
                    MachineName = m2Obj.MachineName,
                    Shift = Shift.A,
                    Operator = "Suresh Patil",
                    StartTime = DateTime.Parse("2026-07-27T06:00:00Z").ToUniversalTime(),
                    EndTime = DateTime.Parse("2026-07-27T14:00:00Z").ToUniversalTime(),
                    Capacity = 10m,
                    UtilizationPercent = 70m,
                    DelayMinutes = 45,
                    Status = ScheduleStatus.Delayed,
                    Notes = "Material late from RM store."
                };
                var s4 = new ProductionSchedule
                {
                    ScheduleNumber = "SCH-2026-503",
                    WorkOrderId = wo2Obj.Id,
                    WorkOrderNumber = wo2Obj.WorkOrderNumber,
                    ProductName = wo2Obj.ProductName,
                    MachineId = m1Obj.Id,
                    MachineCode = m1Obj.MachineCode,
                    MachineName = m1Obj.MachineName,
                    Shift = Shift.A,
                    Operator = "Rahul Mehta",
                    StartTime = DateTime.Parse("2026-07-30T06:00:00Z").ToUniversalTime(),
                    EndTime = DateTime.Parse("2026-07-30T14:00:00Z").ToUniversalTime(),
                    Capacity = 8m,
                    UtilizationPercent = 0m,
                    DelayMinutes = 0,
                    Status = ScheduleStatus.Scheduled,
                    Notes = "Tomorrow WO-302 kickoff."
                };
                _dbContext.ProductionSchedules.AddRange(s1, s2, s3, s4);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // ── SCHEDULING METHODS ──

        public async Task<List<ScheduleListItemDto>> GetSchedulesAsync(string? search, string? status, string? shift, int? workOrderId, int? machineId, string? dateFrom, string? dateTo, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync(cancellationToken);

            var q = _dbContext.ProductionSchedules.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.ScheduleNumber.ToLower().Contains(s) ||
                                 x.WorkOrderNumber.ToLower().Contains(s) ||
                                 x.ProductName.ToLower().Contains(s) ||
                                 x.MachineCode.ToLower().Contains(s) ||
                                 x.Operator.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ScheduleStatus>(status, true, out var statusEnum))
                {
                    q = q.Where(x => x.Status == statusEnum);
                }
            }

            if (!string.IsNullOrWhiteSpace(shift))
            {
                if (Enum.TryParse<Shift>(shift, true, out var shiftEnum))
                {
                    q = q.Where(x => x.Shift == shiftEnum);
                }
            }

            if (workOrderId.HasValue)
            {
                q = q.Where(x => x.WorkOrderId == workOrderId.Value);
            }

            if (machineId.HasValue)
            {
                q = q.Where(x => x.MachineId == machineId.Value);
            }

            if (!string.IsNullOrWhiteSpace(dateFrom))
            {
                var dtFrom = DateTime.Parse(dateFrom).ToUniversalTime();
                q = q.Where(x => x.StartTime >= dtFrom);
            }

            if (!string.IsNullOrWhiteSpace(dateTo))
            {
                var dtTo = DateTime.Parse(dateTo).ToUniversalTime();
                q = q.Where(x => x.StartTime <= dtTo);
            }

            var items = await q.OrderByDescending(x => x.StartTime).ToListAsync(cancellationToken);

            return items.Select(s => new ScheduleListItemDto
            {
                Id = s.Id,
                ScheduleNumber = s.ScheduleNumber,
                WorkOrderNumber = s.WorkOrderNumber,
                ProductName = s.ProductName,
                MachineCode = s.MachineCode,
                MachineName = s.MachineName,
                Shift = s.Shift.ToString(),
                Operator = s.Operator,
                StartTime = s.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UtilizationPercent = s.UtilizationPercent,
                Status = s.Status.ToString()
            }).ToList();
        }

        public async Task<ScheduleDto?> GetScheduleByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync(cancellationToken);

            var s = await _dbContext.ProductionSchedules
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (s is null) return null;

            var timeline = new List<ProductionTimelineEventDto>
            {
                new()
                {
                    Id = $"sch-tl-created-{s.Id}",
                    Date = s.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = s.CreatedBy,
                    Action = "Scheduled",
                    ToStatus = ScheduleStatus.Scheduled.ToString()
                }
            };
            if (s.Status != ScheduleStatus.Scheduled)
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"sch-tl-status-{s.Id}",
                    Date = s.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = s.UpdatedBy,
                    Action = $"Status → {s.Status}",
                    ToStatus = s.Status.ToString()
                });
            }

            return new ScheduleDto
            {
                Id = s.Id,
                ScheduleNumber = s.ScheduleNumber,
                WorkOrderId = s.WorkOrderId,
                WorkOrderNumber = s.WorkOrderNumber,
                ProductName = s.ProductName,
                MachineId = s.MachineId,
                MachineCode = s.MachineCode,
                MachineName = s.MachineName,
                Shift = s.Shift.ToString(),
                Operator = s.Operator,
                StartTime = s.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                EndTime = s.EndTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Capacity = s.Capacity,
                UtilizationPercent = s.UtilizationPercent,
                DelayMinutes = s.DelayMinutes,
                Status = s.Status.ToString(),
                Notes = s.Notes,
                Timeline = timeline,
                CreatedBy = s.CreatedBy,
                CreatedAt = s.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedBy = s.UpdatedBy,
                UpdatedAt = s.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public async Task<ScheduleDashboardDto> GetScheduleDashboardAsync(CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync(cancellationToken);

            var list = await _dbContext.ProductionSchedules.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            var today = DateTime.UtcNow.Date;
            var todaysCount = list.Count(x => x.StartTime.Date == today);
            var running = list.Count(x => x.Status == ScheduleStatus.Running);
            var upcoming = list.Count(x => x.Status == ScheduleStatus.Scheduled);
            var delayed = list.Count(x => x.Status == ScheduleStatus.Delayed);

            return new ScheduleDashboardDto
            {
                TodaysSchedule = todaysCount,
                RunningJobs = running,
                UpcomingJobs = upcoming,
                DelayedJobs = delayed
            };
        }

        // ── MACHINE METHODS ──

        public async Task<List<MachineListItemDto>> GetMachinesAsync(string? search, string? status, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync(cancellationToken);

            var q = _dbContext.Machines.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.MachineCode.ToLower().Contains(s) ||
                                 x.MachineName.ToLower().Contains(s) ||
                                 x.Department.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<MachineStatus>(status, true, out var statusEnum))
                {
                    q = q.Where(x => x.Status == statusEnum);
                }
            }

            var items = await q.OrderBy(x => x.MachineCode).ToListAsync(cancellationToken);
            var machineIds = items.Select(x => x.Id).ToList();

            var workOrderCounts = await _dbContext.WorkOrders
                .Where(x => !x.IsDeleted && x.MachineId.HasValue && machineIds.Contains(x.MachineId.Value))
                .GroupBy(x => x.MachineId!.Value)
                .Select(g => new { MachineId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MachineId, x => x.Count, cancellationToken);

            return items.Select(m => new MachineListItemDto
            {
                Id = m.Id,
                MachineCode = m.MachineCode,
                MachineName = m.MachineName,
                Department = m.Department,
                RunningHours = m.RunningHours,
                IdleHours = m.IdleHours,
                BreakdownHours = m.BreakdownHours,
                UtilizationPercent = m.UtilizationPercent,
                MaintenanceDue = m.MaintenanceDue.ToString("yyyy-MM-dd"),
                Status = m.Status.ToString(),
                AssignedWorkOrderCount = workOrderCounts.TryGetValue(m.Id, out var count) ? count : 0
            }).ToList();
        }

        public async Task<MachineDto?> GetMachineByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync(cancellationToken);

            var m = await _dbContext.Machines
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (m is null) return null;

            var assignedWos = await _dbContext.WorkOrders
                .Where(x => !x.IsDeleted && x.MachineId == id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var timeline = new List<ProductionTimelineEventDto>
            {
                new()
                {
                    Id = $"mch-tl-created-{m.Id}",
                    Date = m.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = m.CreatedBy,
                    Action = "Machine registered",
                    ToStatus = "Idle"
                }
            };
            if (m.Status != MachineStatus.Idle)
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"mch-tl-status-{m.Id}",
                    Date = m.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = m.UpdatedBy,
                    Action = $"Status → {m.Status}",
                    ToStatus = m.Status.ToString()
                });
            }

            return new MachineDto
            {
                Id = m.Id,
                MachineCode = m.MachineCode,
                MachineName = m.MachineName,
                Department = m.Department,
                RunningHours = m.RunningHours,
                IdleHours = m.IdleHours,
                BreakdownHours = m.BreakdownHours,
                UtilizationPercent = m.UtilizationPercent,
                MaintenanceDue = m.MaintenanceDue.ToString("yyyy-MM-dd"),
                Status = m.Status.ToString(),
                AssignedWorkOrderIds = assignedWos,
                Notes = m.Notes,
                Timeline = timeline,
                CreatedBy = m.CreatedBy,
                CreatedAt = m.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedBy = m.UpdatedBy,
                UpdatedAt = m.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public async Task<MachineDashboardDto> GetMachineDashboardAsync(CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync(cancellationToken);

            var list = await _dbContext.Machines.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
            var running = list.Count(x => x.Status == MachineStatus.Running);
            var idle = list.Count(x => x.Status == MachineStatus.Idle);
            var breakdown = list.Count(x => x.Status == MachineStatus.Breakdown);
            var avgUtil = list.Count > 0 ? list.Average(x => x.UtilizationPercent) : 0m;

            return new MachineDashboardDto
            {
                RunningMachines = running,
                IdleMachines = idle,
                Breakdown = breakdown,
                AverageUtilization = Math.Round(avgUtil)
            };
        }

        // ── PRODUCTION ENTRY METHODS ──

        public async Task<List<EntryListItemDto>> GetEntriesAsync(string? search, string? status, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.ProductionEntries.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.EntryNumber.ToLower().Contains(s) ||
                                 x.WorkOrderNumber.ToLower().Contains(s) ||
                                 x.ProductCode.ToLower().Contains(s) ||
                                 x.ProductName.ToLower().Contains(s) ||
                                 x.Operator.ToLower().Contains(s) ||
                                 x.MachineName.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<EntryStatus>(status, true, out var statusEnum))
                {
                    q = q.Where(x => x.Status == statusEnum);
                }
            }

            var items = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
            return items.Select(MapToEntryListItemDto).ToList();
        }

        public async Task<EntryDto?> GetEntryByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var e = await _dbContext.ProductionEntries
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (e is null) return null;

            // Fetch dynamic timeline events
            var timeline = new List<ProductionTimelineEventDto>
            {
                new()
                {
                    Id = $"entry-tl-created-{e.Id}",
                    Date = e.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = e.CreatedBy,
                    Action = "Created",
                    ToStatus = EntryStatus.Draft.ToString()
                }
            };

            if (e.Status == EntryStatus.Approved || e.Status == EntryStatus.Posted)
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"entry-tl-approved-{e.Id}",
                    Date = e.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = e.UpdatedBy,
                    Action = "Approved",
                    ToStatus = EntryStatus.Approved.ToString()
                });
            }

            if (e.Status == EntryStatus.Posted)
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"entry-tl-posted-{e.Id}",
                    Date = e.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = e.UpdatedBy,
                    Action = "Posted",
                    ToStatus = EntryStatus.Posted.ToString()
                });
            }

            if (e.Notes.Contains("[Finished Goods generated]"))
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"entry-tl-fg-{e.Id}",
                    Date = e.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = e.UpdatedBy,
                    Action = "Finished Goods generated and posted to inventory",
                    Remarks = "Good quantity posted to Finished Goods store."
                });
            }

            var hasConsumption = await _dbContext.MaterialConsumptions
                .AnyAsync(x => x.EntryId == e.Id && !x.IsDeleted, cancellationToken);
            if (hasConsumption)
            {
                timeline.Insert(0, new ProductionTimelineEventDto
                {
                    Id = $"entry-tl-mc-{e.Id}",
                    Date = e.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = e.UpdatedBy,
                    Action = "Material consumption generated",
                    Remarks = "Material consumption generated and Store stock reduced."
                });
            }

            return MapToEntryDto(e, timeline);
        }

        public async Task<EntryDto> CreateEntryAsync(EntryCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var wo = await _dbContext.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == request.WorkOrderId && !x.IsDeleted, cancellationToken);
            if (wo is null)
            {
                throw new InvalidOperationException("Work Order not found");
            }

            var machine = await _dbContext.Machines
                .FirstOrDefaultAsync(x => x.Id == request.MachineId && !x.IsDeleted, cancellationToken);
            if (machine is null)
            {
                throw new InvalidOperationException("Machine not found");
            }

            if (request.ProducedQuantity <= 0)
            {
                throw new InvalidOperationException("Produced quantity must be greater than zero");
            }

            if (request.GoodQuantity < 0 || request.RejectedQuantity < 0)
            {
                throw new InvalidOperationException("Good and Rejected quantities cannot be negative");
            }

            if (request.GoodQuantity + request.RejectedQuantity != request.ProducedQuantity)
            {
                throw new InvalidOperationException("Produced quantity must equal Good + Rejected quantity");
            }

            if (!Enum.TryParse<Shift>(request.Shift, true, out var shiftEnum))
            {
                throw new InvalidOperationException("Invalid Shift");
            }

            if (string.IsNullOrWhiteSpace(request.Operator))
            {
                throw new InvalidOperationException("Operator is required");
            }

            var prodDate = DateOnly.Parse(request.ProductionDate);

            var entryNum = await GenerateDocNumberAsync("ENT", cancellationToken);

            var entry = new ProductionEntry
            {
                EntryNumber = entryNum,
                WorkOrderId = wo.Id,
                WorkOrderNumber = wo.WorkOrderNumber,
                ProductId = wo.ProductId,
                ProductCode = wo.ProductCode,
                ProductName = wo.ProductName,
                ProducedQuantity = request.ProducedQuantity,
                GoodQuantity = request.GoodQuantity,
                RejectedQuantity = request.RejectedQuantity,
                Shift = shiftEnum,
                Operator = request.Operator.Trim(),
                MachineId = machine.Id,
                MachineCode = machine.MachineCode,
                MachineName = machine.MachineName,
                ProductionDate = prodDate,
                Status = EntryStatus.Draft,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedBy = actingUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = actingUser,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.ProductionEntries.Add(entry);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var initialTimeline = new List<ProductionTimelineEventDto>
            {
                new()
                {
                    Id = $"entry-tl-created-{entry.Id}",
                    Date = entry.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = entry.CreatedBy,
                    Action = "Created",
                    ToStatus = EntryStatus.Draft.ToString()
                }
            };

            return MapToEntryDto(entry, initialTimeline);
        }

        public async Task<EntryDto?> UpdateEntryAsync(int id, EntryUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            var entry = await _dbContext.ProductionEntries
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entry is null) return null;

            if (entry.Status != EntryStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft production entries can be updated");
            }

            var wo = await _dbContext.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == (request.WorkOrderId ?? entry.WorkOrderId) && !x.IsDeleted, cancellationToken);
            if (wo is null)
            {
                throw new InvalidOperationException("Work Order not found");
            }

            var machine = await _dbContext.Machines
                .FirstOrDefaultAsync(x => x.Id == request.MachineId && !x.IsDeleted, cancellationToken);
            if (machine is null)
            {
                throw new InvalidOperationException("Machine not found");
            }

            if (request.ProducedQuantity <= 0)
            {
                throw new InvalidOperationException("Produced quantity must be greater than zero");
            }

            if (request.GoodQuantity < 0 || request.RejectedQuantity < 0)
            {
                throw new InvalidOperationException("Good and Rejected quantities cannot be negative");
            }

            if (request.GoodQuantity + request.RejectedQuantity != request.ProducedQuantity)
            {
                throw new InvalidOperationException("Produced quantity must equal Good + Rejected quantity");
            }

            if (!Enum.TryParse<Shift>(request.Shift, true, out var shiftEnum))
            {
                throw new InvalidOperationException("Invalid Shift");
            }

            if (string.IsNullOrWhiteSpace(request.Operator))
            {
                throw new InvalidOperationException("Operator is required");
            }

            var prodDate = DateOnly.Parse(request.ProductionDate);

            entry.WorkOrderId = wo.Id;
            entry.WorkOrderNumber = wo.WorkOrderNumber;
            entry.ProductId = wo.ProductId;
            entry.ProductCode = wo.ProductCode;
            entry.ProductName = wo.ProductName;
            entry.ProducedQuantity = request.ProducedQuantity;
            entry.GoodQuantity = request.GoodQuantity;
            entry.RejectedQuantity = request.RejectedQuantity;
            entry.Shift = shiftEnum;
            entry.Operator = request.Operator.Trim();
            entry.MachineId = machine.Id;
            entry.MachineCode = machine.MachineCode;
            entry.MachineName = machine.MachineName;
            entry.ProductionDate = prodDate;
            entry.Notes = request.Notes?.Trim() ?? string.Empty;
            entry.UpdatedBy = actingUser;
            entry.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetEntryByIdAsync(entry.Id, cancellationToken);
        }

        public async Task<EntryDto?> ApproveEntryAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            var entry = await _dbContext.ProductionEntries
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entry is null) return null;

            if (entry.Status != EntryStatus.Draft && entry.Status != EntryStatus.Submitted)
            {
                throw new InvalidOperationException($"Cannot approve entry in status {entry.Status}");
            }

            if (entry.ProducedQuantity <= 0 || entry.GoodQuantity + entry.RejectedQuantity != entry.ProducedQuantity)
            {
                throw new InvalidOperationException("Cannot approve entry with invalid quantities");
            }

            entry.Status = EntryStatus.Approved;
            entry.UpdatedBy = actingUser;
            entry.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(payload?.Remarks))
            {
                entry.Notes = string.IsNullOrWhiteSpace(entry.Notes) ? payload.Remarks : $"{entry.Notes}\n[{actingUser}]: {payload.Remarks}";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return await GetEntryByIdAsync(entry.Id, cancellationToken);
        }

        public async Task<EntryDto?> PostEntryAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default)
        {
            var entry = await _dbContext.ProductionEntries
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entry is null) return null;

            if (entry.Status != EntryStatus.Approved)
            {
                throw new InvalidOperationException($"Cannot post entry in status {entry.Status}");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                entry.Status = EntryStatus.Posted;
                entry.UpdatedBy = actingUser;
                entry.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(payload?.Remarks))
                {
                    entry.Notes = string.IsNullOrWhiteSpace(entry.Notes) ? payload.Remarks : $"{entry.Notes}\n[{actingUser}]: {payload.Remarks}";
                }

                // Update Work Order production quantities
                var wo = await _dbContext.WorkOrders
                    .FirstOrDefaultAsync(x => x.Id == entry.WorkOrderId && !x.IsDeleted, cancellationToken);
                if (wo != null)
                {
                    wo.ProducedQuantity += entry.GoodQuantity;
                    wo.PendingQuantity = Math.Max(0m, wo.PlannedQuantity - wo.ProducedQuantity);
                    wo.UpdatedBy = actingUser;
                    wo.UpdatedAt = DateTime.UtcNow;
                    wo.Notes = string.IsNullOrWhiteSpace(wo.Notes) 
                        ? $"Entry {entry.EntryNumber} posted: +{entry.GoodQuantity} good" 
                        : $"{wo.Notes}\n[{actingUser}]: Entry {entry.EntryNumber} posted: +{entry.GoodQuantity} good";
                }

                // Record Quality Rejection if rejected > 0
                if (entry.RejectedQuantity > 0)
                {
                    var rejNum = await GenerateDocNumberAsync("RJ", cancellationToken);
                    var rejection = new RejectionRecord
                    {
                        RejectionNumber = rejNum,
                        WorkOrderId = entry.WorkOrderId,
                        WorkOrderNumber = entry.WorkOrderNumber,
                        EntryId = entry.Id,
                        ProductId = entry.ProductId,
                        ProductCode = entry.ProductCode,
                        ProductName = entry.ProductName,
                        Quantity = entry.RejectedQuantity,
                        Reason = "Quality rejection recorded from production entry",
                        Category = "Process",
                        Operator = entry.Operator,
                        MachineId = entry.MachineId,
                        MachineCode = entry.MachineCode,
                        MachineName = entry.MachineName,
                        RejectionDate = entry.ProductionDate,
                        CorrectiveAction = "Pending review",
                        Notes = $"Auto-generated from {entry.EntryNumber}",
                        CreatedBy = actingUser,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedBy = actingUser,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.RejectionRecords.Add(rejection);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return await GetEntryByIdAsync(entry.Id, cancellationToken);
        }

        public async Task<EntryDashboardDto> GetEntryDashboardAsync(CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var list = await _dbContext.ProductionEntries
                .Where(x => !x.IsDeleted && x.ProductionDate == today)
                .ToListAsync(cancellationToken);

            var total = list.Sum(x => x.ProducedQuantity);
            var good = list.Sum(x => x.GoodQuantity);
            var rejected = list.Sum(x => x.RejectedQuantity);
            var productivity = total > 0 ? (good / total) * 100m : 0m;

            return new EntryDashboardDto
            {
                TodaysProduction = total,
                GoodQuantity = good,
                RejectedQuantity = rejected,
                Productivity = Math.Round(productivity, 1)
            };
        }

        public async Task<List<ConsumptionDto>> GenerateMaterialConsumptionAsync(int entryId, string actingUser, CancellationToken cancellationToken = default)
        {
            var entry = await _dbContext.ProductionEntries
                .FirstOrDefaultAsync(x => x.Id == entryId && !x.IsDeleted, cancellationToken);

            if (entry is null)
            {
                throw new InvalidOperationException("Entry not found");
            }

            if (entry.Status != EntryStatus.Approved && entry.Status != EntryStatus.Posted)
            {
                throw new InvalidOperationException("Entry must be Approved or Posted to record material consumption");
            }

            if (entry.ProducedQuantity <= 0)
            {
                throw new InvalidOperationException("Cannot consume materials for zero production quantity");
            }

            var existing = await _dbContext.MaterialConsumptions
                .Where(x => x.EntryId == entryId && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0)
            {
                throw new InvalidOperationException($"Material consumption already recorded for {entry.EntryNumber} ({existing.Count} lines).");
            }

            var wo = await _dbContext.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == entry.WorkOrderId && !x.IsDeleted, cancellationToken);
            if (wo is null)
            {
                throw new InvalidOperationException("Work order not found");
            }

            var bom = await _dbContext.BillOfMaterials
                .Include(x => x.Materials)
                .FirstOrDefaultAsync(x => x.Id == wo.BomId && !x.IsDeleted, cancellationToken);
            if (bom is null)
            {
                throw new InvalidOperationException("BOM not found");
            }

            var createdList = new List<MaterialConsumption>();

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var m in bom.Materials)
                {
                    var plannedQuantity = Math.Round(m.Quantity * entry.ProducedQuantity, 2);
                    var actualQuantity = Math.Round(plannedQuantity * (1 + m.WastagePercent / 100), 2);
                    var variance = Math.Round(actualQuantity - plannedQuantity, 2);

                    var stockOutRequest = new StockOutRequestDto
                    {
                        MaterialId = m.MaterialId,
                        WarehouseId = m.WarehouseId,
                        Quantity = actualQuantity,
                        Reason = "Production material consumption",
                        ReferenceType = "ProductionOrder",
                        ReferenceNumber = wo.WorkOrderNumber,
                        Remarks = $"{entry.EntryNumber} · WO {wo.WorkOrderNumber}"
                    };

                    var txn = await _inventoryService.StockOutAsync(stockOutRequest, actingUser, cancellationToken);

                    var mc = new MaterialConsumption
                    {
                        ConsumptionNumber = await GenerateDocNumberAsync("MC", cancellationToken),
                        WorkOrderId = wo.Id,
                        WorkOrderNumber = wo.WorkOrderNumber,
                        BomId = bom.Id,
                        BomNumber = bom.BomNumber,
                        EntryId = entry.Id,
                        MaterialId = m.MaterialId,
                        MaterialCode = m.MaterialCode,
                        MaterialName = m.MaterialName,
                        PlannedQuantity = plannedQuantity,
                        ActualQuantity = actualQuantity,
                        Variance = variance,
                        Uom = m.Uom,
                        WarehouseId = m.WarehouseId,
                        WarehouseName = m.WarehouseName,
                        StockOutReference = txn.TransactionNumber,
                        Notes = $"Linked Store txn {txn.TransactionNumber} from {entry.EntryNumber}",
                        CreatedBy = actingUser,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedBy = actingUser,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _dbContext.MaterialConsumptions.Add(mc);
                    createdList.Add(mc);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                foreach (var mc in createdList)
                {
                    mc.BatchNumber = $"BATCH-{mc.MaterialCode.Substring(Math.Max(0, mc.MaterialCode.Length - 3)).ToUpper()}-AUTO{mc.Id}";
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return createdList.Select(MapToConsumptionDto).ToList();
        }

        public async Task<object> GenerateFinishedGoodsAsync(int entryId, string actingUser, CancellationToken cancellationToken = default)
        {
            var entry = await _dbContext.ProductionEntries
                .FirstOrDefaultAsync(x => x.Id == entryId && !x.IsDeleted, cancellationToken);

            if (entry is null)
            {
                throw new InvalidOperationException("Entry not found");
            }

            if (entry.Status != EntryStatus.Approved && entry.Status != EntryStatus.Posted)
            {
                throw new InvalidOperationException("Entry must be Approved or Posted to generate finished goods");
            }

            if (entry.GoodQuantity <= 0)
            {
                throw new InvalidOperationException("Good quantity must be greater than zero");
            }

            if (entry.Notes.Contains("[Finished Goods generated]"))
            {
                throw new InvalidOperationException($"Finished goods already generated for {entry.EntryNumber}");
            }

            var inspection = await _dbContext.FinalInspections
                .FirstOrDefaultAsync(x => x.ProductionEntryId == entryId && !x.IsDeleted, cancellationToken);
            if (inspection is null)
            {
                throw new InvalidOperationException("Cannot generate finished goods without a final inspection record.");
            }

            if (inspection.Status != ERP.Domain.Procurement.FinalInspectionStatus.Approved && 
                inspection.Status != ERP.Domain.Procurement.FinalInspectionStatus.Closed)
            {
                throw new InvalidOperationException($"Cannot generate finished goods. Final inspection status is '{inspection.Status}', but must be Approved.");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var adjustRequest = new FinishedGoodAdjustRequestDto
                {
                    ProductId = entry.ProductId,
                    QuantityDelta = entry.GoodQuantity,
                    Reason = "Finished goods from production entry",
                    Remarks = $"{entry.EntryNumber} · WO {entry.WorkOrderNumber}"
                };

                var res = await _inventoryService.AdjustFinishedGoodAsync(adjustRequest, actingUser, cancellationToken);
                if (res is null)
                {
                    throw new InvalidOperationException($"No finished-goods SKU found for {entry.ProductCode}");
                }

                entry.Notes = string.IsNullOrWhiteSpace(entry.Notes)
                    ? "[Finished Goods generated]"
                    : $"{entry.Notes}\n[Finished Goods generated]";
                entry.UpdatedBy = actingUser;
                entry.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new
                {
                    message = $"{entry.GoodQuantity} {res.Unit} of {res.ProductName} ({res.ProductCode}) posted to Finished Goods Inventory.",
                    quantity = entry.GoodQuantity
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // ── MATERIAL CONSUMPTION METHODS ──

        public async Task<List<ConsumptionListItemDto>> GetConsumptionsAsync(string? search, int? workOrderId, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.MaterialConsumptions.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.ConsumptionNumber.ToLower().Contains(s) ||
                                 x.WorkOrderNumber.ToLower().Contains(s) ||
                                 x.BomNumber.ToLower().Contains(s) ||
                                 x.MaterialCode.ToLower().Contains(s) ||
                                 x.MaterialName.ToLower().Contains(s));
            }

            if (workOrderId.HasValue)
            {
                q = q.Where(x => x.WorkOrderId == workOrderId.Value);
            }

            var items = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
            return items.Select(MapToConsumptionListItemDto).ToList();
        }

        public async Task<ConsumptionDto?> GetConsumptionByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var mc = await _dbContext.MaterialConsumptions
                .Include(x => x.Entry)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return mc is null ? null : MapToConsumptionDto(mc);
        }

        public async Task<ConsumptionDashboardDto> GetConsumptionDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.MaterialConsumptions.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);

            var totalPlanned = list.Sum(x => x.PlannedQuantity);
            var totalActual = list.Sum(x => x.ActualQuantity);
            var totalVariance = list.Sum(x => x.Variance);

            return new ConsumptionDashboardDto
            {
                PlannedConsumption = totalPlanned,
                ActualConsumption = totalActual,
                Variance = totalVariance
            };
        }

        // ── MAPPER HELPERS FOR ENTRIES & CONSUMPTIONS ──

        private static EntryListItemDto MapToEntryListItemDto(ProductionEntry e)
        {
            return new EntryListItemDto
            {
                Id = e.Id,
                EntryNumber = e.EntryNumber,
                WorkOrderNumber = e.WorkOrderNumber,
                ProductCode = e.ProductCode,
                ProductName = e.ProductName,
                ProducedQuantity = e.ProducedQuantity,
                GoodQuantity = e.GoodQuantity,
                RejectedQuantity = e.RejectedQuantity,
                Shift = e.Shift.ToString(),
                Operator = e.Operator,
                MachineName = e.MachineName,
                ProductionDate = e.ProductionDate.ToString("yyyy-MM-dd"),
                Status = e.Status.ToString()
            };
        }

        private static EntryDto MapToEntryDto(ProductionEntry e, List<ProductionTimelineEventDto> timeline)
        {
            return new EntryDto
            {
                Id = e.Id,
                EntryNumber = e.EntryNumber,
                WorkOrderId = e.WorkOrderId,
                WorkOrderNumber = e.WorkOrderNumber,
                ProductId = e.ProductId,
                ProductCode = e.ProductCode,
                ProductName = e.ProductName,
                ProducedQuantity = e.ProducedQuantity,
                GoodQuantity = e.GoodQuantity,
                RejectedQuantity = e.RejectedQuantity,
                Shift = e.Shift.ToString(),
                Operator = e.Operator,
                MachineId = e.MachineId,
                MachineCode = e.MachineCode,
                MachineName = e.MachineName,
                ProductionDate = e.ProductionDate.ToString("yyyy-MM-dd"),
                Status = e.Status.ToString(),
                Notes = e.Notes,
                Attachments = new List<ProductionAttachmentDto>(),
                Timeline = timeline,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedBy = e.UpdatedBy,
                UpdatedAt = e.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        private static ConsumptionListItemDto MapToConsumptionListItemDto(MaterialConsumption mc)
        {
            return new ConsumptionListItemDto
            {
                Id = mc.Id,
                ConsumptionNumber = mc.ConsumptionNumber,
                WorkOrderNumber = mc.WorkOrderNumber,
                BomNumber = mc.BomNumber,
                MaterialCode = mc.MaterialCode,
                MaterialName = mc.MaterialName,
                PlannedQuantity = mc.PlannedQuantity,
                ActualQuantity = mc.ActualQuantity,
                Variance = mc.Variance,
                Uom = mc.Uom,
                WarehouseName = mc.WarehouseName,
                BatchNumber = mc.BatchNumber,
                StockOutReference = mc.StockOutReference
            };
        }

        private static ConsumptionDto MapToConsumptionDto(MaterialConsumption mc)
        {
            var timeline = new List<ProductionTimelineEventDto>
            {
                new()
                {
                    Id = $"cons-tl-{mc.Id}",
                    Date = mc.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    User = mc.CreatedBy,
                    Action = "Recorded",
                    Remarks = $"Linked stock-out txn: {mc.StockOutReference}"
                }
            };

            return new ConsumptionDto
            {
                Id = mc.Id,
                ConsumptionNumber = mc.ConsumptionNumber,
                WorkOrderId = mc.WorkOrderId,
                WorkOrderNumber = mc.WorkOrderNumber,
                BomId = mc.BomId,
                BomNumber = mc.BomNumber,
                EntryId = mc.EntryId,
                MaterialId = mc.MaterialId,
                MaterialCode = mc.MaterialCode,
                MaterialName = mc.MaterialName,
                PlannedQuantity = mc.PlannedQuantity,
                ActualQuantity = mc.ActualQuantity,
                Variance = mc.Variance,
                Uom = mc.Uom,
                WarehouseId = mc.WarehouseId,
                WarehouseName = mc.WarehouseName,
                BatchNumber = mc.BatchNumber,
                StockOutReference = mc.StockOutReference,
                Notes = mc.Notes,
                CreatedBy = mc.CreatedBy,
                CreatedAt = mc.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedBy = mc.UpdatedBy,
                UpdatedAt = mc.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Timeline = timeline
            };
        }
    }
}
