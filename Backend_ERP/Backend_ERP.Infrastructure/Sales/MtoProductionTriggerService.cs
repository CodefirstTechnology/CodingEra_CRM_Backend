using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Procurement;
using ERP.Domain.Production;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class MtoProductionTriggerService : IMtoProductionTriggerService
    {
        private readonly ERPDbContext _db;

        public MtoProductionTriggerService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<MtoEvaluationResultDto> EvaluateAndSpawnProductionAsync(
            int salesOrderId,
            string actingUser,
            CancellationToken ct = default)
        {
            var order = await _db.SalesOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == salesOrderId, ct);

            if (order == null)
            {
                return new MtoEvaluationResultDto
                {
                    SalesOrderId = salesOrderId,
                    IsMtoRequired = false,
                    Details = new List<string> { $"Sales Order #{salesOrderId} not found." }
                };
            }

            var spawnedPlanIds = new List<int>();
            var details = new List<string>();

            foreach (var line in order.Items.Where(i => i.ItemId.HasValue && i.ItemType == PricingItemType.FinishedGood))
            {
                var fg = await _db.FinishedGoods.FirstOrDefaultAsync(x => x.Id == line.ItemId!.Value, ct);
                if (fg == null) continue;

                // 1. Available physical on-hand stock (Unreserved ATP)
                var availablePhysical = fg.AvailableQuantity;

                // 2. Open Work Orders pending production
                var openWoPending = await _db.WorkOrders
                    .Where(w => w.ProductId == fg.Id &&
                                w.Status != WorkOrderStatus.Completed &&
                                w.Status != WorkOrderStatus.Closed)
                    .SumAsync(w => (decimal?)w.PendingQuantity, ct) ?? 0m;

                // 3. Existing unallocated committed demand from other active Sales Orders
                var otherCommittedDemand = await _db.SalesOrderItems
                    .Where(i => i.ItemId == fg.Id &&
                                i.SalesOrderId != order.Id &&
                                (i.SalesOrder.Status == SalesOrderStatuses.Confirmed ||
                                 i.SalesOrder.Status == SalesOrderStatuses.Processing))
                    .SumAsync(i => (decimal?)(i.Quantity - i.AllocatedQuantity), ct) ?? 0m;

                // True Net Deficit Calculation:
                // NetDeficit = OrderedQty - (AvailablePhysical + OpenWorkOrdersPending - ExistingCommittedDemand)
                var netSupply = availablePhysical + Math.Max(0m, openWoPending - otherCommittedDemand);
                var netDeficit = line.Quantity - line.AllocatedQuantity - netSupply;

                if (netDeficit <= 0)
                {
                    details.Add($"Item '{line.ItemCode}': Scheduled supply (ATP {availablePhysical:N2} + Open WOs {openWoPending:N2}) covers demand. No MTO plan spawned.");
                    continue;
                }

                // Locate active BOM for Product
                var bom = await _db.BillOfMaterials
                    .FirstOrDefaultAsync(b => b.ProductId == fg.Id && b.Status == BomStatus.Active, ct)
                    ?? await _db.BillOfMaterials.FirstOrDefaultAsync(b => b.ProductId == fg.Id, ct);

                var planNumber = $"MTO-{order.SalesOrderNumber}-{line.SortIndex + 1:D2}";
                var plan = new ProductionPlan
                {
                    PlanNumber = planNumber,
                    PlanningPeriod = $"{order.OrderDate.Year}-M{order.OrderDate.Month:D2}",
                    ProductId = fg.Id,
                    ProductCode = fg.ProductCode,
                    ProductName = fg.ProductName,
                    RequiredQuantity = netDeficit,
                    PlannedQuantity = netDeficit,
                    BomId = bom?.Id ?? 0,
                    BomNumber = bom?.BomNumber ?? string.Empty,
                    WarehouseId = fg.WarehouseId,
                    WarehouseName = fg.WarehouseName,
                    Priority = Priority.High,
                    Planner = actingUser,
                    ExpectedStart = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExpectedFinish = order.ExpectedDeliveryDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    Status = PlanStatus.Draft,
                    Notes = $"Auto-generated MTO for Sales Order #{order.SalesOrderNumber}, Line: {line.ItemCode}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = actingUser,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = actingUser
                };

                _db.ProductionPlans.Add(plan);
                await _db.SaveChangesAsync(ct);

                // Auto-generate linked Work Order
                var workOrder = new WorkOrder
                {
                    WorkOrderNumber = $"WO-{order.SalesOrderNumber}-{line.SortIndex + 1:D2}",
                    PlanId = plan.Id,
                    PlanNumber = plan.PlanNumber,
                    BomId = plan.BomId,
                    BomNumber = plan.BomNumber,
                    ProductId = fg.Id,
                    ProductCode = fg.ProductCode,
                    ProductName = fg.ProductName,
                    PlannedQuantity = netDeficit,
                    ProducedQuantity = 0m,
                    PendingQuantity = netDeficit,
                    Priority = Priority.High,
                    Supervisor = actingUser,
                    StartDate = plan.ExpectedStart,
                    DueDate = plan.ExpectedFinish,
                    Status = WorkOrderStatus.Draft,
                    Notes = $"MTO Work Order spawned for Sales Order #{order.SalesOrderNumber}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = actingUser,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = actingUser
                };

                _db.WorkOrders.Add(workOrder);
                await _db.SaveChangesAsync(ct);

                spawnedPlanIds.Add(plan.Id);
                details.Add($"Item '{line.ItemCode}': Net deficit of {netDeficit:N2} detected. Spawned Production Plan '{plan.PlanNumber}' and Work Order '{workOrder.WorkOrderNumber}'.");
            }

            return new MtoEvaluationResultDto
            {
                SalesOrderId = salesOrderId,
                IsMtoRequired = spawnedPlanIds.Count > 0,
                SpawnedProductionPlanIds = spawnedPlanIds,
                Details = details
            };
        }
    }
}
