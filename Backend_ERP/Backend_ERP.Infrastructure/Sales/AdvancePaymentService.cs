using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Infrastructure.Sales
{
    public class AdvancePaymentService : IAdvancePaymentService
    {
        private readonly IAdvancePaymentRepository _repo;
        private readonly IAdvancePaymentNumberingService _numbering;

        public AdvancePaymentService(
            IAdvancePaymentRepository repo,
            IAdvancePaymentNumberingService numbering)
        {
            _repo = repo;
            _numbering = numbering;
        }

        public async Task<IReadOnlyList<AdvancePaymentListItemDto>> GetAllAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(query, cancellationToken);
            return rows.Select(AdvancePaymentMapper.ToListItem).ToList();
        }

        public async Task<AdvancePaymentDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity is null ? null : AdvancePaymentMapper.ToDto(entity);
        }

        public async Task<AdvancePaymentDto> CreateAsync(
            AdvancePaymentCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateOrUpdate(
                request.CustomerId,
                request.CustomerName,
                request.PaymentDate,
                request.PaymentMode,
                request.ReferenceNumber,
                request.Currency,
                request.AdvanceAmount);

            var paymentMode = AdvancePaymentModeRules.Normalize(request.PaymentMode)
                ?? throw new InvalidOperationException($"Unknown payment mode '{request.PaymentMode}'.");

            var status = AdvancePaymentStatusRules.Normalize(request.Status ?? AdvancePaymentStatuses.Draft)
                ?? AdvancePaymentStatuses.Draft;
            if (status is not (AdvancePaymentStatuses.Draft or AdvancePaymentStatuses.Submitted))
            {
                throw new InvalidOperationException("New advance payments may only start as Draft or Submitted.");
            }

            var number = string.IsNullOrWhiteSpace(request.PaymentNumber)
                ? await _numbering.GenerateNextPaymentNumberAsync(cancellationToken)
                : request.PaymentNumber.Trim();

            if (await _repo.PaymentNumberExistsAsync(number, null, cancellationToken))
            {
                throw new InvalidOperationException($"Payment number '{number}' already exists.");
            }

            var (salesOrderId, salesOrderNumber) = await ResolveSalesOrderAsync(
                request.SalesOrderId,
                request.SalesOrderNumber,
                cancellationToken);

            var advanceAmount = AdvancePaymentCalculator.Round2(request.AdvanceAmount);
            var now = DateTimeOffset.UtcNow;
            var paymentDate = AdvancePaymentMapper.ParseDate(
                request.PaymentDate,
                DateOnly.FromDateTime(DateTime.UtcNow));

            var entity = new AdvancePayment
            {
                PaymentNumber = number,
                CustomerId = request.CustomerId.Trim(),
                CustomerName = request.CustomerName.Trim(),
                SalesOrderId = salesOrderId,
                SalesOrderNumber = salesOrderNumber,
                QuotationId = request.QuotationId,
                QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty,
                PaymentDate = paymentDate,
                PaymentMode = paymentMode,
                ReferenceNumber = request.ReferenceNumber.Trim(),
                Currency = request.Currency.Trim().ToUpperInvariant(),
                ExchangeRate = request.ExchangeRate is > 0 ? request.ExchangeRate.Value : 1m,
                AdvanceAmount = advanceAmount,
                AppliedAmount = 0m,
                RemainingAmount = advanceAmount,
                Status = status,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                AttachmentName = string.IsNullOrWhiteSpace(request.AttachmentName)
                    ? null
                    : request.AttachmentName.Trim(),
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                Timeline =
                [
                    NewTimeline(AdvancePaymentTimelineActions.Created, "Advance payment created", actingUser, now)
                ]
            };

            if (status == AdvancePaymentStatuses.Submitted)
            {
                entity.Timeline.Add(NewTimeline(
                    AdvancePaymentTimelineActions.Submitted,
                    "Submitted on create",
                    actingUser,
                    now));
            }

            await _repo.CreateAsync(entity, cancellationToken);
            return AdvancePaymentMapper.ToDto(
                await _repo.GetByIdAsync(entity.Id, true, false, cancellationToken) ?? entity);
        }

        public async Task<AdvancePaymentDto?> UpdateAsync(
            int id,
            AdvancePaymentUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (entity.Status is not (AdvancePaymentStatuses.Draft or AdvancePaymentStatuses.Rejected))
            {
                throw new InvalidOperationException(
                    $"Cannot update an advance payment in '{entity.Status}' status.");
            }

            ValidateCreateOrUpdate(
                request.CustomerId,
                request.CustomerName,
                request.PaymentDate,
                request.PaymentMode,
                request.ReferenceNumber,
                request.Currency,
                request.AdvanceAmount);

            var paymentMode = AdvancePaymentModeRules.Normalize(request.PaymentMode)
                ?? throw new InvalidOperationException($"Unknown payment mode '{request.PaymentMode}'.");

            if (!string.IsNullOrWhiteSpace(request.PaymentNumber)
                && !string.Equals(request.PaymentNumber.Trim(), entity.PaymentNumber, StringComparison.Ordinal))
            {
                var newNumber = request.PaymentNumber.Trim();
                if (await _repo.PaymentNumberExistsAsync(newNumber, id, cancellationToken))
                {
                    throw new InvalidOperationException($"Payment number '{newNumber}' already exists.");
                }

                entity.PaymentNumber = newNumber;
            }

            var (salesOrderId, salesOrderNumber) = await ResolveSalesOrderAsync(
                request.SalesOrderId,
                request.SalesOrderNumber,
                cancellationToken);

            var advanceAmount = AdvancePaymentCalculator.Round2(request.AdvanceAmount);
            if (advanceAmount < entity.AppliedAmount)
            {
                throw new InvalidOperationException(
                    "Advance amount cannot be less than already applied amount.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.CustomerId = request.CustomerId.Trim();
            entity.CustomerName = request.CustomerName.Trim();
            entity.SalesOrderId = salesOrderId;
            entity.SalesOrderNumber = salesOrderNumber;
            entity.QuotationId = request.QuotationId;
            entity.QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty;
            entity.PaymentDate = AdvancePaymentMapper.ParseDate(request.PaymentDate, entity.PaymentDate);
            entity.PaymentMode = paymentMode;
            entity.ReferenceNumber = request.ReferenceNumber.Trim();
            entity.Currency = request.Currency.Trim().ToUpperInvariant();
            entity.ExchangeRate = request.ExchangeRate is > 0 ? request.ExchangeRate.Value : 1m;
            entity.AdvanceAmount = advanceAmount;
            entity.RemainingAmount = AdvancePaymentCalculator.CalcRemaining(advanceAmount, entity.AppliedAmount);
            entity.Remarks = request.Remarks?.Trim() ?? string.Empty;
            entity.AttachmentName = string.IsNullOrWhiteSpace(request.AttachmentName)
                ? null
                : request.AttachmentName.Trim();
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.Timeline.Add(NewTimeline(
                AdvancePaymentTimelineActions.Updated,
                "Advance payment updated",
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return AdvancePaymentMapper.ToDto(
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

            if (entity.Status != AdvancePaymentStatuses.Draft)
            {
                throw new InvalidOperationException("Only Draft advance payments can be deleted.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.IsDeleted = true;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.Timeline.Add(NewTimeline(
                AdvancePaymentTimelineActions.Deleted,
                "Advance payment deleted",
                actingUser,
                now));

            return await _repo.DeleteAsync(entity, cancellationToken);
        }

        public Task<AdvancePaymentDto?> SubmitAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default) =>
            TransitionAsync(
                id,
                AdvancePaymentStatuses.Submitted,
                AdvancePaymentTimelineActions.Submitted,
                request?.Remarks,
                actingUser,
                cancellationToken);

        public async Task<AdvancePaymentDto?> VerifyAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            EnsureTransition(entity.Status, AdvancePaymentStatuses.FinanceVerification);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = AdvancePaymentStatuses.FinanceVerification;
            entity.VerifiedBy = actingUser;
            entity.VerifiedOn = now;
            entity.Remarks = request?.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.Timeline.Add(NewTimeline(
                AdvancePaymentTimelineActions.Verified,
                BuildRemarks(old, AdvancePaymentStatuses.FinanceVerification, request?.Remarks),
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return AdvancePaymentMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<AdvancePaymentDto?> ReceiveAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            EnsureTransition(entity.Status, AdvancePaymentStatuses.Received);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = AdvancePaymentStatuses.Received;
            entity.ReceivedBy = actingUser;
            entity.ReceivedOn = now;
            entity.Remarks = request?.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.Timeline.Add(NewTimeline(
                AdvancePaymentTimelineActions.Received,
                BuildRemarks(old, AdvancePaymentStatuses.Received, request?.Remarks),
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return AdvancePaymentMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public Task<AdvancePaymentDto?> RejectAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default) =>
            TransitionAsync(
                id,
                AdvancePaymentStatuses.Rejected,
                AdvancePaymentTimelineActions.Rejected,
                request?.Remarks,
                actingUser,
                cancellationToken);

        public Task<AdvancePaymentDto?> CancelAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default) =>
            TransitionAsync(
                id,
                AdvancePaymentStatuses.Cancelled,
                AdvancePaymentTimelineActions.Cancelled,
                request?.Remarks,
                actingUser,
                cancellationToken);

        public async Task<AdvancePaymentDto?> ApplyAsync(
            int id,
            AdvancePaymentApplyRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (!AdvancePaymentStatusRules.CanApply(entity.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot apply payment from status '{entity.Status}'. Payment must be Received or PartiallyApplied.");
            }

            if (request.SalesOrderId <= 0)
            {
                throw new InvalidOperationException("Sales order is required when applying payment.");
            }

            var applyAmount = AdvancePaymentCalculator.Round2(request.ApplyAmount);
            if (applyAmount <= 0)
            {
                throw new InvalidOperationException("Apply amount must be greater than zero.");
            }

            if (AdvancePaymentCalculator.WouldOverAllocate(entity.RemainingAmount, applyAmount))
            {
                throw new InvalidOperationException(
                    $"Apply amount {applyAmount} exceeds remaining amount {entity.RemainingAmount}.");
            }

            if (await _repo.ApplicationExistsAsync(id, request.SalesOrderId, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Advance payment already applied to sales order '{request.SalesOrderId}'.");
            }

            var (salesOrderId, salesOrderNumber) = await ResolveSalesOrderAsync(
                request.SalesOrderId,
                request.SalesOrderNumber,
                cancellationToken);

            if (salesOrderId is null)
            {
                throw new InvalidOperationException("Sales order is required when applying payment.");
            }

            var now = DateTimeOffset.UtcNow;
            var oldStatus = entity.Status;
            entity.AppliedAmount = AdvancePaymentCalculator.Round2(entity.AppliedAmount + applyAmount);
            entity.RemainingAmount = AdvancePaymentCalculator.CalcRemaining(
                entity.AdvanceAmount,
                entity.AppliedAmount);

            if (entity.RemainingAmount < 0)
            {
                throw new InvalidOperationException("Remaining amount cannot be negative.");
            }

            var newStatus = AdvancePaymentCalculator.ResolveStatusAfterApply(entity.RemainingAmount);
            if (!AdvancePaymentStatusRules.CanTransition(oldStatus, newStatus)
                && oldStatus != newStatus)
            {
                throw new InvalidOperationException(
                    $"Cannot transition advance payment from '{oldStatus}' to '{newStatus}'.");
            }

            entity.Status = newStatus;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            var application = new AdvancePaymentApplication
            {
                SalesOrderId = salesOrderId.Value,
                SalesOrderNumber = string.IsNullOrWhiteSpace(salesOrderNumber)
                    ? (request.SalesOrderNumber?.Trim() ?? string.Empty)
                    : salesOrderNumber,
                ApplyAmount = applyAmount,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                AppliedBy = actingUser,
                AppliedOn = now
            };

            entity.Timeline.Add(NewTimeline(
                AdvancePaymentTimelineActions.Applied,
                $"Applied {applyAmount} to {application.SalesOrderNumber} ({oldStatus} → {newStatus})"
                + (string.IsNullOrWhiteSpace(request.Remarks) ? string.Empty : $": {request.Remarks.Trim()}"),
                actingUser,
                now));

            await _repo.ApplyAsync(entity, application, cancellationToken);
            return AdvancePaymentMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<IReadOnlyList<AdvancePaymentTimelineDto>?> GetTimelineAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetTimelineAsync(id, cancellationToken);
            return rows?.Select(AdvancePaymentMapper.ToTimelineDto).ToList();
        }

        public async Task<IReadOnlyList<AdvancePaymentLedgerDto>> GetLedgerAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetLedgerAsync(query, cancellationToken);
            return rows.Select(AdvancePaymentMapper.ToLedgerItem).ToList();
        }

        public async Task<AdvancePaymentDashboardDto> GetDashboardAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetDashboardAsync(cancellationToken);
            return new AdvancePaymentDashboardDto
            {
                TotalCount = rows.Count,
                DraftCount = rows.Count(x => x.Status == AdvancePaymentStatuses.Draft),
                SubmittedCount = rows.Count(x => x.Status == AdvancePaymentStatuses.Submitted),
                FinanceVerificationCount = rows.Count(x => x.Status == AdvancePaymentStatuses.FinanceVerification),
                ReceivedCount = rows.Count(x => x.Status == AdvancePaymentStatuses.Received),
                PartiallyAppliedCount = rows.Count(x => x.Status == AdvancePaymentStatuses.PartiallyApplied),
                FullyAppliedCount = rows.Count(x => x.Status == AdvancePaymentStatuses.FullyApplied),
                CancelledCount = rows.Count(x => x.Status == AdvancePaymentStatuses.Cancelled),
                RejectedCount = rows.Count(x => x.Status == AdvancePaymentStatuses.Rejected),
                TotalAdvanceAmount = rows.Sum(x => x.AdvanceAmount),
                TotalAppliedAmount = rows.Sum(x => x.AppliedAmount),
                TotalRemainingAmount = rows.Sum(x => x.RemainingAmount),
                Recent = rows.OrderByDescending(x => x.UpdatedDate).Take(10)
                    .Select(AdvancePaymentMapper.ToListItem).ToList()
            };
        }

        public async Task<AdvancePaymentReportDto> GetReportsAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await GetAllAsync(query, cancellationToken);
            return new AdvancePaymentReportDto
            {
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O"),
                RowCount = rows.Count,
                TotalAdvanceAmount = rows.Sum(x => x.AdvanceAmount),
                TotalAppliedAmount = rows.Sum(x => x.AppliedAmount),
                TotalRemainingAmount = rows.Sum(x => x.RemainingAmount),
                Rows = rows.ToList()
            };
        }

        public async Task<AdvancePaymentExportMetadataDto> ExportReportsAsync(
            AdvancePaymentExportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var report = await GetReportsAsync(new AdvancePaymentListQueryDto
            {
                Status = request.Status,
                DateFrom = request.DateFrom,
                DateTo = request.DateTo
            }, cancellationToken);

            return new AdvancePaymentExportMetadataDto
            {
                FileName = $"advance-payment-report-{DateTime.UtcNow:yyyyMMddHHmmss}.{(request.Format?.ToLowerInvariant() == "xlsx" ? "xlsx" : "csv")}",
                ContentType = request.Format?.ToLowerInvariant() == "xlsx"
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv",
                Message = $"Export prepared for {report.RowCount} row(s) (placeholder).",
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O")
            };
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "advance-payments.view",
                "advance-payments.create",
                "advance-payments.edit",
                "advance-payments.delete",
                "advance-payments.submit",
                "advance-payments.verify",
                "advance-payments.receive",
                "advance-payments.reject",
                "advance-payments.cancel",
                "advance-payments.apply",
                "advance-payments.dashboard.view",
                "advance-payments.report.view",
                "advance-payments.ledger.view"
            ]);

        public async Task<IReadOnlyList<AdvancePaymentLookupCustomerDto>> LookupCustomersAsync(
            CancellationToken cancellationToken = default)
        {
            var fromPayments = await _repo.GetAllAsync(null, cancellationToken);
            var paymentCustomers = fromPayments
                .GroupBy(x => new { x.CustomerId, x.CustomerName })
                .Select(g => g.Key)
                .Select(x => new { x.CustomerId, x.CustomerName });

            var fromOrders = await _repo.ListSalesOrdersForLookupAsync(cancellationToken);
            var orderCustomers = fromOrders
                .GroupBy(x => x.CustomerName)
                .Select(g => g.Key)
                .Select(name => new { CustomerId = name, CustomerName = name });

            return paymentCustomers
                .Concat(orderCustomers)
                .GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerId) ? x.CustomerName : x.CustomerId)
                .Select(g => g.First())
                .OrderBy(x => x.CustomerName)
                .Take(100)
                .Select(x => new AdvancePaymentLookupCustomerDto
                {
                    Id = string.IsNullOrWhiteSpace(x.CustomerId) ? x.CustomerName : x.CustomerId,
                    Name = x.CustomerName
                })
                .ToList();
        }

        public async Task<IReadOnlyList<AdvancePaymentLookupSalesOrderDto>> LookupSalesOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            var orders = await _repo.ListSalesOrdersForLookupAsync(cancellationToken);
            return orders.Select(o => new AdvancePaymentLookupSalesOrderDto
            {
                Id = o.Id,
                SalesOrderNumber = o.SalesOrderNumber,
                CustomerName = o.CustomerName,
                Status = o.Status
            }).ToList();
        }

        public async Task<IReadOnlyList<AdvancePaymentLookupQuotationDto>> LookupQuotationsAsync(
            CancellationToken cancellationToken = default)
        {
            var payments = await _repo.GetAllAsync(null, cancellationToken);
            var fromPayments = payments
                .Where(x => x.QuotationId != null && !string.IsNullOrWhiteSpace(x.QuotationNumber))
                .Select(x => new { x.QuotationId, x.QuotationNumber, x.CustomerName });

            var orders = await _repo.ListSalesOrdersForLookupAsync(cancellationToken);
            var fromOrders = orders
                .Where(x => x.QuotationId != null && !string.IsNullOrWhiteSpace(x.QuotationNumber))
                .Select(x => new { x.QuotationId, x.QuotationNumber, x.CustomerName });

            return fromPayments.Concat(fromOrders)
                .GroupBy(x => x.QuotationId)
                .Select(g => g.First())
                .OrderByDescending(x => x.QuotationId)
                .Take(100)
                .Select(x => new AdvancePaymentLookupQuotationDto
                {
                    Id = x.QuotationId!.Value,
                    QuotationNumber = x.QuotationNumber,
                    CustomerName = x.CustomerName
                })
                .ToList();
        }

        private async Task<AdvancePaymentDto?> TransitionAsync(
            int id,
            string targetStatus,
            string action,
            string? remarks,
            string actingUser,
            CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            EnsureTransition(entity.Status, targetStatus);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = targetStatus;
            entity.Remarks = remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.Timeline.Add(NewTimeline(
                action,
                BuildRemarks(old, targetStatus, remarks),
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return AdvancePaymentMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        private static void EnsureTransition(string from, string to)
        {
            if (!AdvancePaymentStatusRules.CanTransition(from, to))
            {
                throw new InvalidOperationException(
                    $"Cannot transition advance payment from '{from}' to '{to}'.");
            }
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
                    throw new InvalidOperationException($"Sales Order '{id}' was not found.");
                }

                if (so.Status == SalesOrderStatuses.Cancelled)
                {
                    throw new InvalidOperationException($"Cannot apply payment to Cancelled Sales Order '{so.SalesOrderNumber}'.");
                }

                return (so.Id, so.SalesOrderNumber);
            }

            return (null, salesOrderNumber?.Trim() ?? string.Empty);
        }

        private static void ValidateCreateOrUpdate(
            string customerId,
            string customerName,
            string paymentDate,
            string paymentMode,
            string referenceNumber,
            string currency,
            decimal advanceAmount)
        {
            if (string.IsNullOrWhiteSpace(customerName) && string.IsNullOrWhiteSpace(customerId))
            {
                throw new InvalidOperationException("Customer is required.");
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new InvalidOperationException("Customer name is required.");
            }

            if (string.IsNullOrWhiteSpace(paymentDate))
            {
                throw new InvalidOperationException("Payment date is required.");
            }

            if (string.IsNullOrWhiteSpace(paymentMode))
            {
                throw new InvalidOperationException("Payment mode is required.");
            }

            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                throw new InvalidOperationException("Reference number is required.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new InvalidOperationException("Currency is required.");
            }

            if (advanceAmount <= 0)
            {
                throw new InvalidOperationException("Advance amount must be greater than zero.");
            }
        }

        private static string BuildRemarks(string oldStatus, string newStatus, string? remarks)
        {
            var baseText = $"{oldStatus} → {newStatus}";
            return string.IsNullOrWhiteSpace(remarks) ? baseText : $"{baseText}: {remarks.Trim()}";
        }

        private static AdvancePaymentTimeline NewTimeline(
            string action,
            string remarks,
            string performedBy,
            DateTimeOffset performedOn) => new()
        {
            Action = action,
            Remarks = remarks,
            PerformedBy = performedBy,
            PerformedOn = performedOn
        };
    }
}
