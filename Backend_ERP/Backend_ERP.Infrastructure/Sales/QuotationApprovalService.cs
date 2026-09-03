using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales
{
    public class QuotationApprovalService : IQuotationApprovalService
    {
        private readonly IQuotationApprovalRepository _repo;
        private readonly IQuotationApprovalNumberingService _numbering;
        private readonly ILogger<QuotationApprovalService> _logger;
        private readonly ISalesOrderService _salesOrderService;

        public QuotationApprovalService(
            IQuotationApprovalRepository repo,
            IQuotationApprovalNumberingService numbering,
            ILogger<QuotationApprovalService> logger,
            ISalesOrderService salesOrderService)
        {
            _repo = repo;
            _numbering = numbering;
            _logger = logger;
            _salesOrderService = salesOrderService;
        }

        // ─── Queries ──────────────────────────────────────────────────────────

        public async Task<IReadOnlyList<QuotationApprovalListItemDto>> GetAllAsync(
            QuotationApprovalListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(query, cancellationToken);
            return rows.Select(QuotationApprovalMapper.ToListItem).ToList();
        }

        public async Task<QuotationApprovalDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity is null ? null : QuotationApprovalMapper.ToDto(entity);
        }

        public async Task<IReadOnlyList<QuotationApprovalHistoryDto>?> GetHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetHistoryAsync(id, cancellationToken);
            return rows?.Select(QuotationApprovalMapper.ToHistoryDto).ToList();
        }

        public async Task<IReadOnlyList<QuotationApprovalCommentDto>?> GetCommentsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetCommentsAsync(id, cancellationToken);
            return rows?.Select(QuotationApprovalMapper.ToCommentDto).ToList();
        }

        public async Task<QuotationApprovalStatisticsDto> GetStatisticsAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetForStatisticsAsync(cancellationToken);
            return new QuotationApprovalStatisticsDto
            {
                TotalCount = rows.Count,
                PendingCount = rows.Count(x =>
                    x.Status == QuotationApprovalStatuses.Submitted ||
                    x.Status == QuotationApprovalStatuses.Draft),
                UnderReviewCount = rows.Count(x => x.Status == QuotationApprovalStatuses.UnderReview),
                ApprovedCount = rows.Count(x => x.Status == QuotationApprovalStatuses.Approved),
                RejectedCount = rows.Count(x => x.Status == QuotationApprovalStatuses.Rejected),
                ReturnedCount = rows.Count(x => x.Status == QuotationApprovalStatuses.Returned),
                CancelledCount = rows.Count(x => x.Status == QuotationApprovalStatuses.Cancelled),
                TotalAmount = rows.Sum(x => x.TotalAmount),
                Recent = rows
                    .OrderByDescending(x => x.UpdatedDate)
                    .Take(10)
                    .Select(QuotationApprovalMapper.ToListItem)
                    .ToList()
            };
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "quotation-approvals.view",
                "quotation-approvals.create",
                "quotation-approvals.update",
                "quotation-approvals.delete",
                "quotation-approvals.submit",
                "quotation-approvals.review",
                "quotation-approvals.approve",
                "quotation-approvals.reject",
                "quotation-approvals.return",
                "quotation-approvals.cancel",
                "quotation-approvals.request-revision",
                "quotation-approvals.reopen",
                "quotation-approvals.comment"
            ]);

        public async Task<IReadOnlyList<QuotationApprovalLookupDto>> LookupSalesOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            var orders = await _repo.ListSalesOrdersForLookupAsync(cancellationToken);
            return orders.Select(x => new QuotationApprovalLookupDto
            {
                Id = x.Id,
                Code = x.SalesOrderNumber,
                Name = string.IsNullOrWhiteSpace(x.CustomerName) ? x.SalesOrderNumber : x.CustomerName
            }).ToList();
        }

        // ─── Write ────────────────────────────────────────────────────────────

        public async Task<QuotationApprovalDto> CreateAsync(
            QuotationApprovalCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            ValidateCore(request.CustomerName, request.TotalAmount, request.SalesPersonUserId);

            var level = QuotationApprovalLevelRules.Normalize(request.ApprovalLevel) ?? QuotationApprovalLevels.Manager;
            var priority = QuotationApprovalPriorityRules.Normalize(request.Priority) ?? QuotationApprovalPriorities.Normal;
            var requestDate = DateHelper.ParseDate(request.RequestDate, DateHelper.Today());

            var now = DateTimeOffset.UtcNow;
            var number = await _numbering.GenerateNextApprovalNumberAsync(cancellationToken);
            var autoApprove = level == QuotationApprovalLevels.Auto;
            var status = autoApprove ? QuotationApprovalStatuses.Approved : QuotationApprovalStatuses.Draft;

            var entity = new QuotationApproval
            {
                ApprovalNumber = number,
                RequestDate = requestDate,
                QuotationId = request.QuotationId,
                QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty,
                SalesOrderId = request.SalesOrderId,
                SalesOrderNumber = request.SalesOrderNumber?.Trim() ?? string.Empty,
                CustomerName = request.CustomerName.Trim(),
                SalesPersonUserId = request.SalesPersonUserId,
                TotalAmount = request.TotalAmount,
                ApprovalLevel = level,
                Priority = priority,
                Status = status,
                Reason = request.Reason.Trim(),
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                IsDeleted = false
            };

            entity.History.Add(NewHistory(
                QuotationApprovalHistoryActions.Created,
                string.Empty,
                status,
                autoApprove ? "Created and auto-approved (ApprovalLevel=Auto)" : "Quotation approval created in Draft",
                actingUser,
                now));

            await _repo.CreateAsync(entity, cancellationToken);
            _logger.LogInformation("QuotationApproval {Number} created by {User}", number, actingUser);

            return QuotationApprovalMapper.ToDto(
                await _repo.GetByIdAsync(entity.Id, true, false, cancellationToken) ?? entity);
        }

        public async Task<QuotationApprovalDto> UpdateAsync(
            int id,
            QuotationApprovalUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
                throw new InvalidOperationException($"Quotation approval '{id}' was not found.");

            if (!QuotationApprovalStatusRules.CanBeModified(entity.Status))
                throw new InvalidOperationException($"Cannot update quotation approval in status '{entity.Status}'.");

            ValidateCore(request.CustomerName, request.TotalAmount, request.SalesPersonUserId);

            var level = QuotationApprovalLevelRules.Normalize(request.ApprovalLevel) ?? QuotationApprovalLevels.Manager;
            var priority = QuotationApprovalPriorityRules.Normalize(request.Priority) ?? QuotationApprovalPriorities.Normal;
            var requestDate = DateHelper.ParseDate(request.RequestDate, DateHelper.Today());
            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;

            entity.RequestDate = requestDate;
            entity.QuotationId = request.QuotationId;
            entity.QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty;
            entity.SalesOrderId = request.SalesOrderId;
            entity.SalesOrderNumber = request.SalesOrderNumber?.Trim() ?? string.Empty;
            entity.CustomerName = request.CustomerName.Trim();
            entity.SalesPersonUserId = request.SalesPersonUserId;
            entity.TotalAmount = request.TotalAmount;
            entity.ApprovalLevel = level;
            entity.Priority = priority;
            entity.Reason = request.Reason.Trim();
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            entity.History.Add(NewHistory(
                QuotationApprovalHistoryActions.Updated, old, entity.Status,
                "Quotation approval updated", actingUser, now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return QuotationApprovalMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<bool> DeleteAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
                return false;

            if (!QuotationApprovalStatusRules.CanBeDeleted(entity.Status))
                throw new InvalidOperationException("Only draft approvals can be deleted.");

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.IsDeleted = true;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.History.Add(NewHistory(
                QuotationApprovalHistoryActions.Deleted, old, old, "Soft deleted", actingUser, now));

            await _repo.DeleteAsync(entity, cancellationToken);
            _logger.LogInformation("QuotationApproval {Id} deleted by {User}", id, actingUser);
            return true;
        }

        // ─── Workflow transitions ─────────────────────────────────────────────

        public Task<SalesOrderDto> ConvertToSalesOrderAsync(
            int id,
            string currentUser,
            CancellationToken cancellationToken = default) =>
            _salesOrderService.ConvertQuotationAsync(id, currentUser, cancellationToken);

        public Task<QuotationApprovalDto> SubmitAsync(int id, string actingUser, CancellationToken cancellationToken = default) =>
            TransitionAsync(id, QuotationApprovalStatuses.Submitted, QuotationApprovalHistoryActions.Submitted, null, actingUser, cancellationToken);

        public Task<QuotationApprovalDto> ReviewAsync(int id, QuotationApprovalDecisionRequestDto request, string actingUser, CancellationToken cancellationToken = default) =>
            TransitionAsync(id, QuotationApprovalStatuses.UnderReview, QuotationApprovalHistoryActions.UnderReview, request, actingUser, cancellationToken);

        public Task<QuotationApprovalDto> ApproveAsync(int id, QuotationApprovalDecisionRequestDto request, string actingUser, CancellationToken cancellationToken = default) =>
            TransitionAsync(id, QuotationApprovalStatuses.Approved, QuotationApprovalHistoryActions.Approved, request, actingUser, cancellationToken);

        public Task<QuotationApprovalDto> RejectAsync(int id, QuotationApprovalDecisionRequestDto request, string actingUser, CancellationToken cancellationToken = default) =>
            TransitionAsync(id, QuotationApprovalStatuses.Rejected, QuotationApprovalHistoryActions.Rejected, request, actingUser, cancellationToken);

        public Task<QuotationApprovalDto> ReturnAsync(int id, QuotationApprovalDecisionRequestDto request, string actingUser, CancellationToken cancellationToken = default) =>
            TransitionAsync(id, QuotationApprovalStatuses.Returned, QuotationApprovalHistoryActions.Returned, request, actingUser, cancellationToken);

        public Task<QuotationApprovalDto> CancelAsync(int id, QuotationApprovalDecisionRequestDto request, string actingUser, CancellationToken cancellationToken = default) =>
            TransitionAsync(id, QuotationApprovalStatuses.Cancelled, QuotationApprovalHistoryActions.Cancelled, request, actingUser, cancellationToken);

        public Task<QuotationApprovalDto> RequestRevisionAsync(int id, QuotationApprovalDecisionRequestDto request, string actingUser, CancellationToken cancellationToken = default) =>
            TransitionAsync(id, QuotationApprovalStatuses.RevisionRequired, QuotationApprovalHistoryActions.RevisionRequired, request, actingUser, cancellationToken);

        public async Task<QuotationApprovalDto> ChangeStatusAsync(
            int id,
            string status,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
                throw new InvalidOperationException($"Quotation approval '{id}' was not found.");

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;

            entity.Status = status;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.History.Add(NewHistory(
                QuotationApprovalHistoryActions.Updated, old, status,
                $"Status updated to {status}",
                actingUser, now));

            await _repo.UpdateAsync(entity, cancellationToken);
            _logger.LogInformation("QuotationApproval {Id} status updated {Old} -> {Status} by {User}", id, old, status, actingUser);

            return QuotationApprovalMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public Task<QuotationApprovalDto> ReopenAsync(int id, QuotationApprovalDecisionRequestDto request, string actingUser, CancellationToken cancellationToken = default) =>
            TransitionAsync(id, QuotationApprovalStatuses.Reopened, QuotationApprovalHistoryActions.Reopened, request, actingUser, cancellationToken);

        public async Task<QuotationApprovalCommentDto> AddCommentAsync(
            int id,
            QuotationApprovalCommentRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
                throw new InvalidOperationException("Comment is required.");

            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
                throw new InvalidOperationException($"Quotation approval '{id}' was not found.");

            var now = DateTimeOffset.UtcNow;
            var comment = new QuotationApprovalComment
            {
                Comment = request.Comment.Trim(),
                CommentedBy = actingUser,
                CommentedOn = now
            };

            entity.History.Add(NewHistory(
                QuotationApprovalHistoryActions.CommentAdded,
                entity.Status, entity.Status,
                "Comment added", actingUser, now));

            var saved = await _repo.AddCommentAsync(entity, comment, cancellationToken);
            return QuotationApprovalMapper.ToCommentDto(saved!);
        }

        // ─── Private helpers ──────────────────────────────────────────────────

        private async Task<QuotationApprovalDto> TransitionAsync(
            int id,
            string targetStatus,
            string action,
            QuotationApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
                throw new InvalidOperationException($"Quotation approval '{id}' was not found.");

            EnsureTransition(entity.Status, targetStatus);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;

            entity.Status = targetStatus;
            if (!string.IsNullOrWhiteSpace(request?.Remarks))
                entity.Remarks = request.Remarks.Trim();

            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.History.Add(NewHistory(
                action, old, targetStatus,
                BuildRemarks(old, targetStatus, request?.Remarks),
                actingUser, now));

            await _repo.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation(
                "QuotationApproval {Id} transitioned {From} → {To} by {User}",
                id, old, targetStatus, actingUser);

            return QuotationApprovalMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        private static void ValidateCore(string customerName, decimal totalAmount, int salesPersonUserId)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                throw new InvalidOperationException("Customer name is required.");
            if (totalAmount <= 0)
                throw new InvalidOperationException("Total amount must be greater than zero.");
            if (salesPersonUserId <= 0)
                throw new InvalidOperationException("Sales person user id is required.");
        }

        private static void EnsureTransition(string from, string to)
        {
            bool allowed = to switch
            {
                _ when to == QuotationApprovalStatuses.Submitted => QuotationApprovalStatusRules.CanSubmit(from),
                _ when to == QuotationApprovalStatuses.UnderReview => QuotationApprovalStatusRules.CanReview(from),
                _ when to == QuotationApprovalStatuses.Approved => QuotationApprovalStatusRules.CanApproveReject(from),
                _ when to == QuotationApprovalStatuses.Rejected => QuotationApprovalStatusRules.CanApproveReject(from),
                _ when to == QuotationApprovalStatuses.Returned => QuotationApprovalStatusRules.CanReturn(from),
                _ when to == QuotationApprovalStatuses.Cancelled => QuotationApprovalStatusRules.CanCancel(from),
                _ when to == QuotationApprovalStatuses.RevisionRequired => QuotationApprovalStatusRules.CanRequestRevision(from),
                _ when to == QuotationApprovalStatuses.Reopened => QuotationApprovalStatusRules.CanReopen(from),
                _ => false
            };

            if (!allowed)
                throw new InvalidOperationException(
                    $"Cannot transition quotation approval from '{from}' to '{to}'.");
        }

        private static string BuildRemarks(string oldStatus, string newStatus, string? remarks)
        {
            var baseText = $"{oldStatus} → {newStatus}";
            return string.IsNullOrWhiteSpace(remarks) ? baseText : $"{baseText}: {remarks.Trim()}";
        }

        private static QuotationApprovalHistory NewHistory(
            string action, string oldStatus, string newStatus,
            string remarks, string performedBy, DateTimeOffset performedOn) => new()
        {
            Action = action,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Remarks = remarks,
            PerformedBy = performedBy,
            PerformedOn = performedOn
        };
    }
}
