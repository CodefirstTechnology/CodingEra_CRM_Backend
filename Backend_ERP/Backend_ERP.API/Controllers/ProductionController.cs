using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Production;
using ERP.Application.Production.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/production")]
    public class ProductionController : ControllerBase
    {
        private readonly IProductionService _productionService;

        public ProductionController(IProductionService productionService)
        {
            _productionService = productionService;
        }

        private static string ResolveUser(int? userId) => userId is > 0 ? userId.Value.ToString() : "system";

        // ── BILL OF MATERIALS (BOM) ──

        [HttpGet("boms")]
        public async Task<ActionResult<List<BomListItemDto>>> GetBoms([FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetBomsAsync(search, status, cancellationToken));
        }

        [HttpGet("boms/{id:int}")]
        public async Task<ActionResult<BomDto>> GetBomById(int id, CancellationToken cancellationToken = default)
        {
            var bom = await _productionService.GetBomByIdAsync(id, cancellationToken);
            return bom is null ? NotFound() : Ok(bom);
        }

        [HttpPost("boms")]
        public async Task<ActionResult<BomDto>> CreateBom([FromBody] BomCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _productionService.CreateBomAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetBomById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("boms/{id:int}")]
        public async Task<ActionResult<BomDto>> UpdateBom(int id, [FromBody] BomUpdateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.UpdateBomAsync(id, request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("boms/{id:int}")]
        public async Task<ActionResult> DeleteBom(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var ok = await _productionService.DeleteBomAsync(id, ResolveUser(userId), cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("boms/{id:int}/duplicate")]
        public async Task<ActionResult<BomDto>> DuplicateBom(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var duplicated = await _productionService.DuplicateBomAsync(id, ResolveUser(userId), cancellationToken);
                return duplicated is null ? NotFound() : Ok(duplicated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("boms/{id:int}/approve")]
        public async Task<ActionResult<BomDto>> ApproveBom(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.ApproveBomAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("boms/{id:int}/activate")]
        public async Task<ActionResult<BomDto>> ActivateBom(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.ActivateBomAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("boms/{id:int}/archive")]
        public async Task<ActionResult<BomDto>> ArchiveBom(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.ArchiveBomAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("boms/dashboard")]
        public async Task<ActionResult<BomDashboardDto>> GetBomDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetBomDashboardAsync(cancellationToken));
        }


        // ── PRODUCTION PLANNING ──

        [HttpGet("plans")]
        public async Task<ActionResult<List<PlanListItemDto>>> GetPlans([FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetPlansAsync(search, status, cancellationToken));
        }

        [HttpGet("plans/{id:int}")]
        public async Task<ActionResult<PlanDto>> GetPlanById(int id, CancellationToken cancellationToken = default)
        {
            var plan = await _productionService.GetPlanByIdAsync(id, cancellationToken);
            return plan is null ? NotFound() : Ok(plan);
        }

        [HttpPost("plans")]
        public async Task<ActionResult<PlanDto>> CreatePlan([FromBody] PlanCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _productionService.CreatePlanAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetPlanById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("plans/{id:int}")]
        public async Task<ActionResult<PlanDto>> UpdatePlan(int id, [FromBody] PlanUpdateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.UpdatePlanAsync(id, request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("plans/{id:int}")]
        public async Task<ActionResult> DeletePlan(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var ok = await _productionService.DeletePlanAsync(id, ResolveUser(userId), cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("plans/{id:int}/approve")]
        public async Task<ActionResult<PlanDto>> ApprovePlan(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.ApprovePlanAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("plans/{id:int}/release")]
        public async Task<ActionResult<PlanDto>> ReleasePlan(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.ReleasePlanAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("plans/{planId:int}/generate-work-orders")]
        public async Task<ActionResult<List<WorkOrderDto>>> GenerateWorkOrders(int planId, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _productionService.GenerateWorkOrdersAsync(planId, ResolveUser(userId), cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("plans/dashboard")]
        public async Task<ActionResult<PlanDashboardDto>> GetPlanDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetPlanDashboardAsync(cancellationToken));
        }


        // ── WORK ORDERS ──

        [HttpGet("work-orders")]
        public async Task<ActionResult<List<WorkOrderListItemDto>>> GetWorkOrders([FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetWorkOrdersAsync(search, status, cancellationToken));
        }

        [HttpGet("work-orders/{id:int}")]
        public async Task<ActionResult<WorkOrderDto>> GetWorkOrderById(int id, CancellationToken cancellationToken = default)
        {
            var wo = await _productionService.GetWorkOrderByIdAsync(id, cancellationToken);
            return wo is null ? NotFound() : Ok(wo);
        }

        [HttpPost("work-orders")]
        public async Task<ActionResult<WorkOrderDto>> CreateWorkOrder([FromBody] WorkOrderCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _productionService.CreateWorkOrderAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetWorkOrderById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("work-orders/{id:int}")]
        public async Task<ActionResult<WorkOrderDto>> UpdateWorkOrder(int id, [FromBody] WorkOrderUpdateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.UpdateWorkOrderAsync(id, request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("work-orders/{id:int}")]
        public async Task<ActionResult> DeleteWorkOrder(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var ok = await _productionService.DeleteWorkOrderAsync(id, ResolveUser(userId), cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("work-orders/{id:int}/start")]
        public async Task<ActionResult<WorkOrderDto>> StartWorkOrder(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.StartWorkOrderAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("work-orders/{id:int}/release")]
        public async Task<ActionResult<WorkOrderDto>> ReleaseWorkOrder(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.ReleaseWorkOrderAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("work-orders/{id:int}/pause")]
        public async Task<ActionResult<WorkOrderDto>> PauseWorkOrder(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.PauseWorkOrderAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("work-orders/{id:int}/resume")]
        public async Task<ActionResult<WorkOrderDto>> ResumeWorkOrder(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.ResumeWorkOrderAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("work-orders/{id:int}/complete")]
        public async Task<ActionResult<WorkOrderDto>> CompleteWorkOrder(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.CompleteWorkOrderAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("work-orders/{id:int}/close")]
        public async Task<ActionResult<WorkOrderDto>> CloseWorkOrder(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.CloseWorkOrderAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("work-orders/{workOrderId:int}/generate-entry")]
        public ActionResult<EntryDto> GenerateProductionEntry(int workOrderId)
        {
            return Ok(new EntryDto { WorkOrderId = workOrderId, Status = "Draft", EntryNumber = $"ENT-{DateTime.UtcNow.Year}-{workOrderId:D6}" });
        }

        [HttpGet("work-orders/dashboard")]
        public async Task<ActionResult<WorkOrderDashboardDto>> GetWorkOrderDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetWorkOrderDashboardAsync(cancellationToken));
        }


        // ── SCHEDULING ──

        [HttpGet("schedules")]
        public async Task<ActionResult<List<ScheduleListItemDto>>> GetSchedules(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? shift,
            [FromQuery] int? workOrderId,
            [FromQuery] int? machineId,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetSchedulesAsync(search, status, shift, workOrderId, machineId, dateFrom, dateTo, cancellationToken));
        }

        [HttpGet("schedules/{id:int}")]
        public async Task<ActionResult<ScheduleDto>> GetScheduleById(int id, CancellationToken cancellationToken = default)
        {
            var schedule = await _productionService.GetScheduleByIdAsync(id, cancellationToken);
            return schedule is null ? NotFound() : Ok(schedule);
        }

        [HttpGet("schedules/dashboard")]
        public async Task<ActionResult<ScheduleDashboardDto>> GetScheduleDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetScheduleDashboardAsync(cancellationToken));
        }


        // ── MACHINES ──

        [HttpGet("machines")]
        public async Task<ActionResult<List<MachineListItemDto>>> GetMachines(
            [FromQuery] string? search,
            [FromQuery] string? status,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetMachinesAsync(search, status, cancellationToken));
        }

        [HttpGet("machines/{id:int}")]
        public async Task<ActionResult<MachineDto>> GetMachineById(int id, CancellationToken cancellationToken = default)
        {
            var machine = await _productionService.GetMachineByIdAsync(id, cancellationToken);
            return machine is null ? NotFound() : Ok(machine);
        }

        [HttpGet("machines/dashboard")]
        public async Task<ActionResult<MachineDashboardDto>> GetMachineDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetMachineDashboardAsync(cancellationToken));
        }


        // ── PRODUCTION ENTRY ──

        [HttpGet("entries")]
        public async Task<ActionResult<List<EntryListItemDto>>> GetEntries([FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetEntriesAsync(search, status, cancellationToken));
        }

        [HttpGet("entries/{id:int}")]
        public async Task<ActionResult<EntryDto>> GetEntryById(int id, CancellationToken cancellationToken = default)
        {
            var entry = await _productionService.GetEntryByIdAsync(id, cancellationToken);
            return entry is null ? NotFound() : Ok(entry);
        }

        [HttpPost("entries")]
        public async Task<ActionResult<EntryDto>> CreateEntry([FromBody] EntryCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _productionService.CreateEntryAsync(request, ResolveUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetEntryById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("entries/{id:int}")]
        public async Task<ActionResult<EntryDto>> UpdateEntry(int id, [FromBody] EntryUpdateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.UpdateEntryAsync(id, request, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("entries/{id:int}/approve")]
        public async Task<ActionResult<EntryDto>> ApproveEntry(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.ApproveEntryAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("entries/{id:int}/post")]
        public async Task<ActionResult<EntryDto>> PostEntry(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _productionService.PostEntryAsync(id, payload, ResolveUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("entries/{entryId:int}/generate-material-consumption")]
        public async Task<ActionResult<List<ConsumptionDto>>> GenerateMaterialConsumption(int entryId, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var list = await _productionService.GenerateMaterialConsumptionAsync(entryId, ResolveUser(userId), cancellationToken);
                return Ok(list);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("entries/{entryId:int}/generate-finished-goods")]
        public async Task<ActionResult<object>> GenerateFinishedGoods(int entryId, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var res = await _productionService.GenerateFinishedGoodsAsync(entryId, ResolveUser(userId), cancellationToken);
                return Ok(res);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("entries/dashboard")]
        public async Task<ActionResult<EntryDashboardDto>> GetEntryDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetEntryDashboardAsync(cancellationToken));
        }


        // ── MATERIAL CONSUMPTION ──

        [HttpGet("consumptions")]
        public async Task<ActionResult<List<ConsumptionListItemDto>>> GetConsumptions([FromQuery] string? search, [FromQuery] int? workOrderId, CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetConsumptionsAsync(search, workOrderId, cancellationToken));
        }

        [HttpGet("consumptions/{id:int}")]
        public async Task<ActionResult<ConsumptionDto>> GetConsumptionById(int id, CancellationToken cancellationToken = default)
        {
            var consumption = await _productionService.GetConsumptionByIdAsync(id, cancellationToken);
            return consumption is null ? NotFound() : Ok(consumption);
        }

        [HttpPost("consumptions/record/{entryId:int}")]
        public async Task<ActionResult<List<ConsumptionDto>>> RecordConsumption(int entryId, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var list = await _productionService.GenerateMaterialConsumptionAsync(entryId, ResolveUser(userId), cancellationToken);
                return Ok(list);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("consumptions/dashboard")]
        public async Task<ActionResult<ConsumptionDashboardDto>> GetConsumptionDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _productionService.GetConsumptionDashboardAsync(cancellationToken));
        }


        // ── REJECTIONS ──

        [HttpGet("rejections")]
        public ActionResult<List<RejectionListItemDto>> GetRejections([FromQuery] string? search, [FromQuery] string? status)
        {
            return Ok(new List<RejectionListItemDto>());
        }

        [HttpGet("rejections/{id:int}")]
        public ActionResult<RejectionDto> GetRejectionById(int id)
        {
            return Ok(new RejectionDto { Id = id });
        }

        [HttpPost("rejections/record/{entryId:int}")]
        public ActionResult<RejectionDto> RecordRejection(int entryId)
        {
            return Ok(new RejectionDto { EntryId = entryId });
        }

        [HttpGet("rejections/dashboard")]
        public ActionResult<RejectionDashboardDto> GetRejectionDashboard()
        {
            return Ok(new RejectionDashboardDto());
        }


        // ── REPORTS ──

        [HttpGet("reports/daily")]
        public ActionResult<DailyReportDto> GetDailyReport([FromQuery] string? date, [FromQuery] string? shift)
        {
            return Ok(new DailyReportDto());
        }

        [HttpGet("reports/monthly")]
        public ActionResult<MonthlySummaryDto> GetMonthlySummary([FromQuery] string? month)
        {
            return Ok(new MonthlySummaryDto());
        }

        [HttpGet("reports/dashboard")]
        public ActionResult<ReportDashboardDto> GetReportDashboard()
        {
            return Ok(new ReportDashboardDto());
        }
    }
}
