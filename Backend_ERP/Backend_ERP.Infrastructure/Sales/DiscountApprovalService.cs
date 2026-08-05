using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Infrastructure.Sales
{
    public class DiscountApprovalService : IDiscountApprovalService
    {
        private readonly IDiscountApprovalRepository _repo;
        private readonly IDiscountApprovalNumberingService _numbering;

        public DiscountApprovalService(
            IDiscountApprovalRepository repo,
            IDiscountApprovalNumberingService numbering)
        {
            _repo = repo;
            _numbering = numbering;
        }

        public async Task<IReadOnlyList<DiscountApprovalListItemDto>> GetAllAsync(
            DiscountApprovalListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(query, cancellationToken);
            return rows.Select(DiscountApprovalMapper.ToListItem).ToList();
        }

        public async Task<DiscountApprovalDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity is null ? null : DiscountApprovalMapper.ToDto(entity);
        }

        public async Task<DiscountApprovalDto> CreateAsync(
            DiscountApprovalCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var (sourceType, category, level, priority, requestDate) =
                ValidateAndNormalizeCreate(request);

            var (salesOrderId, salesOrderNumber) = await ResolveSalesOrderAsync(
                request.SalesOrderId, request.SalesOrderNumber, cancellationToken);
            var priceListId = await ResolvePriceListAsync(request.PriceListId, cancellationToken);

            if (sourceType == DiscountApprovalSourceTypes.SalesOrder && salesOrderId is null)
            {
                throw new InvalidOperationException(
                    "Sales order reference is required when source type is SalesOrder.");
            }

            if (sourceType == DiscountApprovalSourceTypes.Quotation
                && request.QuotationId is null
                && string.IsNullOrWhiteSpace(request.QuotationNumber))
            {
                throw new InvalidOperationException(
                    "Quotation reference is required when source type is Quotation.");
            }

            ValidateApprovalMatrix(request.RequestedDiscountPercentage, level);

            var now = DateTimeOffset.UtcNow;
            var number = await _numbering.GenerateNextApprovalNumberAsync(cancellationToken);
            var autoApprove = level == DiscountApprovalLevels.Auto;
            var status = autoApprove
                ? DiscountApprovalStatuses.Approved
                : DiscountApprovalStatuses.Pending;

            var entity = new DiscountApproval
            {
                ApprovalNumber = number,
                RequestDate = requestDate,
                SourceType = sourceType,
                QuotationId = request.QuotationId,
                QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty,
                SalesOrderId = salesOrderId,
                SalesOrderNumber = salesOrderNumber,
                PriceListId = priceListId,
                CustomerName = request.CustomerName.Trim(),
                CustomerCategory = category,
                SalesPersonUserId = request.SalesPersonUserId,
                RequestedDiscountPercentage = request.RequestedDiscountPercentage,
                RequestedAmount = request.RequestedAmount,
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

            if (autoApprove)
            {
                entity.ApprovedDiscountPercentage = request.RequestedDiscountPercentage;
                entity.ApprovedAmount = request.RequestedAmount;
            }

            entity.History.Add(NewHistory(
                DiscountApprovalHistoryActions.Created,
                string.Empty,
                status,
                autoApprove
                    ? "Created and auto-approved (ApprovalLevel=Auto)"
                    : "Discount approval request created",
                actingUser,
                now));

            if (autoApprove)
            {
                entity.History.Add(NewHistory(
                    DiscountApprovalHistoryActions.Approved,
                    DiscountApprovalStatuses.Pending,
                    DiscountApprovalStatuses.Approved,
                    "Auto-approved per approval matrix",
                    actingUser,
                    now.AddSeconds(1)));
            }

            await _repo.CreateAsync(entity, cancellationToken);
            return DiscountApprovalMapper.ToDto(
                await _repo.GetByIdAsync(entity.Id, true, false, cancellationToken) ?? entity);
        }

        public async Task<DiscountApprovalDto?> UpdateAsync(
            int id,
            DiscountApprovalUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (!DiscountApprovalStatusRules.IsEditable(entity.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot update discount approval in status '{entity.Status}'.");
            }

            var (sourceType, category, level, priority, requestDate) =
                ValidateAndNormalizeUpdate(request);

            var (salesOrderId, salesOrderNumber) = await ResolveSalesOrderAsync(
                request.SalesOrderId, request.SalesOrderNumber, cancellationToken);
            var priceListId = await ResolvePriceListAsync(request.PriceListId, cancellationToken);

            ValidateApprovalMatrix(request.RequestedDiscountPercentage, level);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;

            entity.RequestDate = requestDate;
            entity.SourceType = sourceType;
            entity.QuotationId = request.QuotationId;
            entity.QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty;
            entity.SalesOrderId = salesOrderId;
            entity.SalesOrderNumber = salesOrderNumber;
            entity.PriceListId = priceListId;
            entity.CustomerName = request.CustomerName.Trim();
            entity.CustomerCategory = category;
            entity.SalesPersonUserId = request.SalesPersonUserId;
            entity.RequestedDiscountPercentage = request.RequestedDiscountPercentage;
            entity.RequestedAmount = request.RequestedAmount;
            entity.ApprovalLevel = level;
            entity.Priority = priority;
            entity.Reason = request.Reason.Trim();
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            entity.History.Add(NewHistory(
                DiscountApprovalHistoryActions.Updated,
                old,
                entity.Status,
                "Discount approval updated",
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return DiscountApprovalMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<bool> DeleteAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            if (DiscountApprovalStatusRules.IsTerminal(entity.Status)
                && entity.Status == DiscountApprovalStatuses.Approved)
            {
                throw new InvalidOperationException("Approved discount approvals cannot be deleted.");
            }

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.IsDeleted = true;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.History.Add(NewHistory(
                DiscountApprovalHistoryActions.Deleted,
                old,
                old,
                "Soft deleted",
                actingUser,
                now));

            await _repo.DeleteAsync(entity, cancellationToken);
            return true;
        }

        public Task<DiscountApprovalDto?> ApproveAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default) =>
            TransitionAsync(
                id,
                DiscountApprovalStatuses.Approved,
                DiscountApprovalHistoryActions.Approved,
                request,
                actingUser,
                markApproved: true,
                moveToUnderReviewFirst: true,
                cancellationToken);

        public Task<DiscountApprovalDto?> RejectAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default) =>
            TransitionAsync(
                id,
                DiscountApprovalStatuses.Rejected,
                DiscountApprovalHistoryActions.Rejected,
                request,
                actingUser,
                markApproved: false,
                moveToUnderReviewFirst: true,
                cancellationToken);

        public Task<DiscountApprovalDto?> ReturnAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default) =>
            TransitionAsync(
                id,
                DiscountApprovalStatuses.Returned,
                DiscountApprovalHistoryActions.Returned,
                request,
                actingUser,
                markApproved: false,
                moveToUnderReviewFirst: false,
                cancellationToken);

        public Task<DiscountApprovalDto?> CancelAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default) =>
            TransitionAsync(
                id,
                DiscountApprovalStatuses.Cancelled,
                DiscountApprovalHistoryActions.Cancelled,
                request,
                actingUser,
                markApproved: false,
                moveToUnderReviewFirst: false,
                cancellationToken);

        public async Task<DiscountApprovalDto?> ResubmitAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            EnsureTransition(entity.Status, DiscountApprovalStatuses.Pending);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = DiscountApprovalStatuses.Pending;
            entity.ApprovedDiscountPercentage = null;
            entity.ApprovedAmount = null;
            if (!string.IsNullOrWhiteSpace(request?.Remarks))
            {
                entity.Remarks = request.Remarks.Trim();
            }

            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.History.Add(NewHistory(
                DiscountApprovalHistoryActions.Resubmitted,
                old,
                DiscountApprovalStatuses.Pending,
                BuildRemarks(old, DiscountApprovalStatuses.Pending, request?.Remarks),
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return DiscountApprovalMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<DiscountApprovalStatisticsDto> GetStatisticsAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetForStatisticsAsync(cancellationToken);
            var approvedAmounts = rows
                .Where(x => x.ApprovedAmount is not null)
                .Select(x => x.ApprovedAmount!.Value)
                .ToList();

            return new DiscountApprovalStatisticsDto
            {
                TotalCount = rows.Count,
                PendingCount = rows.Count(x => x.Status == DiscountApprovalStatuses.Pending),
                UnderReviewCount = rows.Count(x => x.Status == DiscountApprovalStatuses.UnderReview),
                ApprovedCount = rows.Count(x => x.Status == DiscountApprovalStatuses.Approved),
                RejectedCount = rows.Count(x => x.Status == DiscountApprovalStatuses.Rejected),
                ReturnedCount = rows.Count(x => x.Status == DiscountApprovalStatuses.Returned),
                CancelledCount = rows.Count(x => x.Status == DiscountApprovalStatuses.Cancelled),
                TotalRequestedAmount = rows.Sum(x => x.RequestedAmount),
                TotalApprovedAmount = approvedAmounts.Sum(),
                AverageRequestedDiscount = rows.Count == 0
                    ? 0
                    : Math.Round(rows.Average(x => x.RequestedDiscountPercentage), 2),
                Recent = rows
                    .OrderByDescending(x => x.UpdatedDate)
                    .Take(10)
                    .Select(DiscountApprovalMapper.ToListItem)
                    .ToList()
            };
        }

        public async Task<IReadOnlyList<DiscountApprovalHistoryDto>?> GetHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetHistoryAsync(id, cancellationToken);
            return rows?.Select(DiscountApprovalMapper.ToHistoryDto).ToList();
        }

        public async Task<IReadOnlyList<DiscountApprovalCommentDto>?> GetCommentsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetCommentsAsync(id, cancellationToken);
            return rows?.Select(DiscountApprovalMapper.ToCommentDto).ToList();
        }

        public async Task<DiscountApprovalCommentDto?> AddCommentAsync(
            int id,
            DiscountApprovalCommentRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
            {
                throw new InvalidOperationException("Comment is required.");
            }

            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            var comment = new DiscountApprovalComment
            {
                Comment = request.Comment.Trim(),
                CommentedBy = actingUser,
                CommentedOn = now
            };

            entity.History.Add(NewHistory(
                DiscountApprovalHistoryActions.CommentAdded,
                entity.Status,
                entity.Status,
                "Comment added",
                actingUser,
                now));

            var saved = await _repo.AddCommentAsync(entity, comment, cancellationToken);
            return saved is null ? null : DiscountApprovalMapper.ToCommentDto(saved);
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "discount-approvals.view",
                "discount-approvals.create",
                "discount-approvals.update",
                "discount-approvals.delete",
                "discount-approvals.approve",
                "discount-approvals.reject",
                "discount-approvals.return",
                "discount-approvals.cancel",
                "discount-approvals.resubmit",
                "discount-approvals.comment"
            ]);

        public async Task<IReadOnlyList<DiscountApprovalLookupDto>> LookupPriceListsAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.ListPriceListsForLookupAsync(cancellationToken);
            return rows.Select(x => new DiscountApprovalLookupDto
            {
                Id = x.Id,
                Code = x.PriceListNumber,
                Name = x.PriceListName
            }).ToList();
        }

        public async Task<IReadOnlyList<DiscountApprovalLookupDto>> LookupSalesOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.ListSalesOrdersForLookupAsync(cancellationToken);
            return rows.Select(x => new DiscountApprovalLookupDto
            {
                Id = x.Id,
                Code = x.SalesOrderNumber,
                Name = string.IsNullOrWhiteSpace(x.CustomerName) ? x.SalesOrderNumber : x.CustomerName
            }).ToList();
        }

        public async Task<IReadOnlyList<DiscountApprovalLookupDto>> LookupQuotationsAsync(
            CancellationToken cancellationToken = default)
        {
            var fromOrders = await _repo.ListSalesOrdersForLookupAsync(cancellationToken);
            return fromOrders
                .Where(x => x.QuotationId is not null || !string.IsNullOrWhiteSpace(x.QuotationNumber))
                .GroupBy(x => x.QuotationId ?? 0)
                .Select(g =>
                {
                    var first = g.First();
                    return new DiscountApprovalLookupDto
                    {
                        Id = first.QuotationId ?? 0,
                        Code = first.QuotationNumber,
                        Name = first.CustomerName
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Code) || x.Id > 0)
                .Take(100)
                .ToList();
        }

        private async Task<DiscountApprovalDto?> TransitionAsync(
            int id,
            string targetStatus,
            string action,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            bool markApproved,
            bool moveToUnderReviewFirst,
            CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;

            if (moveToUnderReviewFirst
                && entity.Status == DiscountApprovalStatuses.Pending
                && targetStatus is DiscountApprovalStatuses.Approved or DiscountApprovalStatuses.Rejected)
            {
                EnsureTransition(entity.Status, DiscountApprovalStatuses.UnderReview);
                var pending = entity.Status;
                entity.Status = DiscountApprovalStatuses.UnderReview;
                entity.UpdatedBy = actingUser;
                entity.UpdatedDate = now;
                entity.History.Add(NewHistory(
                    DiscountApprovalHistoryActions.UnderReview,
                    pending,
                    DiscountApprovalStatuses.UnderReview,
                    "Moved to under review",
                    actingUser,
                    now));
                now = now.AddSeconds(1);
            }

            EnsureTransition(entity.Status, targetStatus);

            var old = entity.Status;
            entity.Status = targetStatus;
            if (!string.IsNullOrWhiteSpace(request?.Remarks))
            {
                entity.Remarks = request.Remarks.Trim();
            }

            if (markApproved)
            {
                entity.ApprovedDiscountPercentage =
                    request?.ApprovedDiscountPercentage ?? entity.RequestedDiscountPercentage;
                entity.ApprovedAmount =
                    request?.ApprovedAmount ?? entity.RequestedAmount;

                if (entity.ApprovedDiscountPercentage is < 0 or > 100)
                {
                    throw new InvalidOperationException(
                        "Approved discount percentage must be between 0 and 100.");
                }

                if (entity.ApprovedAmount is < 0)
                {
                    throw new InvalidOperationException("Approved amount cannot be negative.");
                }
            }

            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.History.Add(NewHistory(
                action,
                old,
                targetStatus,
                BuildRemarks(old, targetStatus, request?.Remarks),
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return DiscountApprovalMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        private async Task<(int? SalesOrderId, string SalesOrderNumber)> ResolveSalesOrderAsync(
            int? salesOrderId,
            string? salesOrderNumber,
            CancellationToken cancellationToken)
        {
            if (salesOrderId is int id and > 0)
            {
                var so = await _repo.FindSalesOrderAsync(id, cancellationToken);
                if (so is null)
                {
                    throw new InvalidOperationException($"Sales order '{id}' was not found.");
                }

                return (so.Id, so.SalesOrderNumber);
            }

            return (null, salesOrderNumber?.Trim() ?? string.Empty);
        }

        private async Task<int?> ResolvePriceListAsync(
            int? priceListId,
            CancellationToken cancellationToken)
        {
            if (priceListId is null or <= 0)
            {
                return null;
            }

            var id = priceListId.Value;
            var pl = await _repo.FindPriceListAsync(id, cancellationToken);
            if (pl is null)
            {
                throw new InvalidOperationException($"Price list '{id}' was not found.");
            }

            return pl.Id;
        }

        private static (string SourceType, string Category, string Level, string Priority, DateOnly RequestDate)
            ValidateAndNormalizeCreate(DiscountApprovalCreateRequestDto request)
        {
            ValidateCore(
                request.CustomerName,
                request.CustomerCategory,
                request.SourceType,
                request.Reason,
                request.RequestedDiscountPercentage,
                request.RequestedAmount,
                request.SalesPersonUserId);

            var sourceType = DiscountApprovalSourceTypeRules.Normalize(request.SourceType)
                ?? throw new InvalidOperationException($"Unknown source type '{request.SourceType}'.");

            var category = PriceListCustomerCategoryRules.Normalize(request.CustomerCategory)
                ?? throw new InvalidOperationException(
                    $"Unknown customer category '{request.CustomerCategory}'.");

            var required = DiscountApprovalLevelRules.RequiredLevelForDiscount(
                request.RequestedDiscountPercentage);
            var level = DiscountApprovalLevelRules.Normalize(request.ApprovalLevel) ?? required;
            var priority = DiscountApprovalPriorityRules.Normalize(request.Priority)
                ?? DiscountApprovalPriorities.Normal;
            var requestDate = DiscountApprovalMapper.ParseDate(
                request.RequestDate,
                DateOnly.FromDateTime(DateTime.UtcNow));

            return (sourceType, category, level, priority, requestDate);
        }

        private static (string SourceType, string Category, string Level, string Priority, DateOnly RequestDate)
            ValidateAndNormalizeUpdate(DiscountApprovalUpdateRequestDto request)
        {
            ValidateCore(
                request.CustomerName,
                request.CustomerCategory,
                request.SourceType,
                request.Reason,
                request.RequestedDiscountPercentage,
                request.RequestedAmount,
                request.SalesPersonUserId);

            var sourceType = DiscountApprovalSourceTypeRules.Normalize(request.SourceType)
                ?? throw new InvalidOperationException($"Unknown source type '{request.SourceType}'.");

            var category = PriceListCustomerCategoryRules.Normalize(request.CustomerCategory)
                ?? throw new InvalidOperationException(
                    $"Unknown customer category '{request.CustomerCategory}'.");

            var required = DiscountApprovalLevelRules.RequiredLevelForDiscount(
                request.RequestedDiscountPercentage);
            var level = DiscountApprovalLevelRules.Normalize(request.ApprovalLevel) ?? required;
            var priority = DiscountApprovalPriorityRules.Normalize(request.Priority)
                ?? DiscountApprovalPriorities.Normal;
            var requestDate = DiscountApprovalMapper.ParseDate(
                request.RequestDate,
                DateOnly.FromDateTime(DateTime.UtcNow));

            return (sourceType, category, level, priority, requestDate);
        }

        private static void ValidateCore(
            string customerName,
            string customerCategory,
            string sourceType,
            string reason,
            decimal discountPercentage,
            decimal requestedAmount,
            int salesPersonUserId)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new InvalidOperationException("Customer name is required.");
            }

            if (string.IsNullOrWhiteSpace(customerCategory))
            {
                throw new InvalidOperationException("Customer category is required.");
            }

            if (string.IsNullOrWhiteSpace(sourceType))
            {
                throw new InvalidOperationException("Source type is required.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Reason is required.");
            }

            if (discountPercentage is < 0 or > 100)
            {
                throw new InvalidOperationException("Discount percentage must be between 0 and 100.");
            }

            if (requestedAmount <= 0)
            {
                throw new InvalidOperationException("Requested amount must be greater than zero.");
            }

            if (salesPersonUserId <= 0)
            {
                throw new InvalidOperationException("Sales person user id is required.");
            }
        }

        private static void ValidateApprovalMatrix(decimal discountPercentage, string level)
        {
            var required = DiscountApprovalLevelRules.RequiredLevelForDiscount(discountPercentage);
            if (!DiscountApprovalLevelRules.LevelSatisfies(level, required))
            {
                throw new InvalidOperationException(
                    $"Approval level '{level}' is insufficient for {discountPercentage}% discount. " +
                    $"Minimum required level is '{required}'.");
            }
        }

        private static void EnsureTransition(string from, string to)
        {
            if (!DiscountApprovalStatusRules.CanTransition(from, to))
            {
                throw new InvalidOperationException(
                    $"Cannot transition discount approval from '{from}' to '{to}'.");
            }
        }

        private static string BuildRemarks(string oldStatus, string newStatus, string? remarks)
        {
            var baseText = $"{oldStatus} → {newStatus}";
            return string.IsNullOrWhiteSpace(remarks) ? baseText : $"{baseText}: {remarks.Trim()}";
        }

        private static DiscountApprovalHistory NewHistory(
            string action,
            string oldStatus,
            string newStatus,
            string remarks,
            string performedBy,
            DateTimeOffset performedOn) => new()
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
