using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class ProformaInvoiceService : IProformaInvoiceService
    {
        private readonly IProformaInvoiceRepository _repo;
        private readonly ERPDbContext _context;

        public ProformaInvoiceService(IProformaInvoiceRepository repo, ERPDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<IReadOnlyList<ProformaInvoiceListItemDto>> GetAllAsync(
            ProformaInvoiceListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var q = ApplyFilters(_repo.Query().AsNoTracking(), query);
            var rows = await q
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
            return rows.Select(ProformaInvoiceMapper.ToListItem).ToList();
        }

        public async Task<ProformaInvoiceDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity is null ? null : ProformaInvoiceMapper.ToDto(entity);
        }

        public async Task<ProformaInvoiceDto> CreateAsync(
            ProformaInvoiceCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request.Customer, request.Items, request.InvoiceDate, request.ValidUntil, request.Currency);

            var now = DateTimeOffset.UtcNow;
            var invoiceDate = ProformaInvoiceMapper.ParseDate(request.InvoiceDate, DateOnly.FromDateTime(DateTime.UtcNow));
            var validUntil = ProformaInvoiceMapper.ParseDate(request.ValidUntil, invoiceDate.AddDays(30));
            if (validUntil < invoiceDate)
            {
                throw new InvalidOperationException("ValidUntil must be on or after InvoiceDate.");
            }

            var status = ProformaInvoiceStatusRules.Normalize(request.Status ?? ProformaInvoiceStatuses.Draft)
                ?? ProformaInvoiceStatuses.Draft;
            if (status is not (ProformaInvoiceStatuses.Draft or ProformaInvoiceStatuses.Submitted))
            {
                throw new InvalidOperationException("New proforma invoices may only start as Draft or Submitted.");
            }

            var number = string.IsNullOrWhiteSpace(request.PiNumber)
                ? await _repo.ReserveNextPiNumberAsync(cancellationToken)
                : request.PiNumber.Trim();

            if (await _repo.PiNumberExistsAsync(number, null, cancellationToken))
            {
                throw new InvalidOperationException($"PI number '{number}' already exists.");
            }

            var (salesOrderId, salesOrderNumber) = await ResolveSalesOrderAsync(
                request.SalesOrderId,
                request.SalesOrderNumber,
                cancellationToken);

            var prepared = PrepareItems(request.Items);
            var totals = ProformaInvoiceCalculator.Summarize(prepared);
            var salesPersonUserId = ParseUserId(request.SalesPersonId, actingUser);

            var entity = new ProformaInvoice
            {
                PiNumber = number,
                InvoiceDate = invoiceDate,
                ValidUntil = validUntil,
                CustomerId = request.Customer.CustomerId?.Trim() ?? string.Empty,
                CustomerName = request.Customer.CustomerName.Trim(),
                ContactPerson = request.Customer.ContactPerson?.Trim() ?? string.Empty,
                BillingAddress = request.Customer.BillingAddress?.Trim() ?? string.Empty,
                ShippingAddress = request.Customer.ShippingAddress?.Trim() ?? string.Empty,
                SalesPersonUserId = salesPersonUserId,
                SalesPerson = string.IsNullOrWhiteSpace(request.SalesPerson)
                    ? salesPersonUserId.ToString()
                    : request.SalesPerson.Trim(),
                Currency = request.Currency.Trim().ToUpperInvariant(),
                ExchangeRate = request.ExchangeRate is > 0 ? request.ExchangeRate.Value : 1m,
                QuotationId = request.QuotationId,
                QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty,
                SalesOrderId = salesOrderId,
                SalesOrderNumber = salesOrderNumber,
                PaymentTerms = request.PaymentTerms?.Trim() ?? string.Empty,
                DeliveryTerms = request.DeliveryTerms?.Trim() ?? string.Empty,
                CustomerNotes = request.CustomerNotes?.Trim() ?? string.Empty,
                InternalNotes = request.InternalNotes?.Trim() ?? string.Empty,
                Subtotal = totals.Subtotal,
                DiscountTotal = totals.DiscountTotal,
                TaxTotal = totals.TaxTotal,
                GrandTotal = totals.GrandTotal,
                Status = status,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                Items = MapItems(prepared),
                StatusHistory =
                [
                    NewStatusHistory(null, status, "Proforma invoice created", actingUser, now)
                ]
            };

            if (status == ProformaInvoiceStatuses.Submitted)
            {
                entity.ApprovalHistory.Add(NewApproval(
                    ProformaApprovalLevels.Manager,
                    ProformaApprovalDecisions.Submit,
                    "Submitted for approval",
                    actingUser,
                    now));
            }

            await _repo.AddAsync(entity, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return ProformaInvoiceMapper.ToDto(
                await _repo.GetByIdAsync(entity.Id, true, false, cancellationToken) ?? entity);
        }

        public async Task<ProformaInvoiceDto?> UpdateAsync(
            int id,
            ProformaInvoiceUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (entity.Status is ProformaInvoiceStatuses.Converted
                or ProformaInvoiceStatuses.Cancelled
                or ProformaInvoiceStatuses.Expired)
            {
                throw new InvalidOperationException($"Cannot update a proforma in '{entity.Status}' status.");
            }

            ValidateRequest(request.Customer, request.Items, request.InvoiceDate, request.ValidUntil, request.Currency);

            var invoiceDate = ProformaInvoiceMapper.ParseDate(request.InvoiceDate, entity.InvoiceDate);
            var validUntil = ProformaInvoiceMapper.ParseDate(request.ValidUntil, entity.ValidUntil);
            if (validUntil < invoiceDate)
            {
                throw new InvalidOperationException("ValidUntil must be on or after InvoiceDate.");
            }

            if (!string.IsNullOrWhiteSpace(request.PiNumber)
                && !string.Equals(request.PiNumber.Trim(), entity.PiNumber, StringComparison.Ordinal))
            {
                var newNumber = request.PiNumber.Trim();
                if (await _repo.PiNumberExistsAsync(newNumber, id, cancellationToken))
                {
                    throw new InvalidOperationException($"PI number '{newNumber}' already exists.");
                }

                entity.PiNumber = newNumber;
            }

            var (salesOrderId, salesOrderNumber) = await ResolveSalesOrderAsync(
                request.SalesOrderId,
                request.SalesOrderNumber,
                cancellationToken);

            var prepared = PrepareItems(request.Items);
            var totals = ProformaInvoiceCalculator.Summarize(prepared);
            var salesPersonUserId = ParseUserId(request.SalesPersonId, entity.SalesPersonUserId.ToString());
            var now = DateTimeOffset.UtcNow;

            entity.InvoiceDate = invoiceDate;
            entity.ValidUntil = validUntil;
            entity.CustomerId = request.Customer.CustomerId?.Trim() ?? string.Empty;
            entity.CustomerName = request.Customer.CustomerName.Trim();
            entity.ContactPerson = request.Customer.ContactPerson?.Trim() ?? string.Empty;
            entity.BillingAddress = request.Customer.BillingAddress?.Trim() ?? string.Empty;
            entity.ShippingAddress = request.Customer.ShippingAddress?.Trim() ?? string.Empty;
            entity.SalesPersonUserId = salesPersonUserId;
            entity.SalesPerson = string.IsNullOrWhiteSpace(request.SalesPerson)
                ? salesPersonUserId.ToString()
                : request.SalesPerson.Trim();
            entity.Currency = request.Currency.Trim().ToUpperInvariant();
            entity.ExchangeRate = request.ExchangeRate is > 0 ? request.ExchangeRate.Value : 1m;
            entity.QuotationId = request.QuotationId;
            entity.QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty;
            entity.SalesOrderId = salesOrderId;
            entity.SalesOrderNumber = salesOrderNumber;
            entity.PaymentTerms = request.PaymentTerms?.Trim() ?? string.Empty;
            entity.DeliveryTerms = request.DeliveryTerms?.Trim() ?? string.Empty;
            entity.CustomerNotes = request.CustomerNotes?.Trim() ?? string.Empty;
            entity.InternalNotes = request.InternalNotes?.Trim() ?? string.Empty;
            entity.Subtotal = totals.Subtotal;
            entity.DiscountTotal = totals.DiscountTotal;
            entity.TaxTotal = totals.TaxTotal;
            entity.GrandTotal = totals.GrandTotal;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            entity.Items.Clear();
            foreach (var item in MapItems(prepared))
            {
                entity.Items.Add(item);
            }

            await _repo.SaveChangesAsync(cancellationToken);
            return ProformaInvoiceMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<bool> DeleteAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, false, true, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            if (entity.Status != ProformaInvoiceStatuses.Draft)
            {
                throw new InvalidOperationException("Only Draft proforma invoices can be deleted.");
            }

            entity.IsDeleted = true;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = DateTimeOffset.UtcNow;
            await _repo.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<ProformaInvoiceDto> GenerateFromSalesOrderAsync(
            int salesOrderId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var salesOrder = await _repo.FindSalesOrderWithItemsAsync(salesOrderId, cancellationToken);
            if (salesOrder is null)
            {
                throw new InvalidOperationException($"Sales Order '{salesOrderId}' was not found.");
            }

            if (salesOrder.Status == SalesOrderStatuses.Draft || salesOrder.Status == SalesOrderStatuses.Cancelled)
            {
                throw new InvalidOperationException(
                    $"Cannot generate Proforma Invoice from Sales Order in status '{salesOrder.Status}'.");
            }

            var existingPi = await _repo.Query().AsNoTracking()
                .FirstOrDefaultAsync(x => x.SalesOrderId == salesOrderId && x.Status != ProformaInvoiceStatuses.Cancelled, cancellationToken);
            if (existingPi is not null)
            {
                throw new InvalidOperationException(
                    $"Proforma Invoice '{existingPi.PiNumber}' already exists for Sales Order '{salesOrder.SalesOrderNumber}'.");
            }

            var items = salesOrder.Items.Select(item => new ProformaInvoiceItemDto
            {
                Id = Guid.NewGuid().ToString("N"),
                ItemName = item.ItemName,
                Description = item.Description ?? string.Empty,
                Quantity = item.Quantity,
                Unit = item.Unit ?? "Nos",
                Rate = item.Rate,
                Discount = item.Discount,
                Gst = item.Gst,
                TaxAmount = item.Amount * (item.Gst / 100m),
                Amount = item.Amount
            }).ToList();

            if (items.Count == 0)
            {
                items.Add(new ProformaInvoiceItemDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ItemName = $"Sales Order #{salesOrder.SalesOrderNumber} Items",
                    Description = salesOrder.Remarks ?? "Sales Order Items",
                    Quantity = 1m,
                    Unit = "Set",
                    Rate = salesOrder.GrandTotal,
                    Discount = 0m,
                    Gst = 18m,
                    TaxAmount = salesOrder.GrandTotal * 0.18m,
                    Amount = salesOrder.GrandTotal
                });
            }

            var request = new ProformaInvoiceCreateRequestDto
            {
                InvoiceDate = DateHelper.FormatDate(DateOnly.FromDateTime(DateTime.UtcNow)),
                ValidUntil = DateHelper.FormatDate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30)),
                Customer = new ProformaInvoiceCustomerDto
                {
                    CustomerId = string.Empty,
                    CustomerName = salesOrder.CustomerName,
                    ContactPerson = salesOrder.ContactPerson ?? string.Empty,
                    BillingAddress = salesOrder.BillingAddress ?? string.Empty,
                    ShippingAddress = salesOrder.ShippingAddress ?? string.Empty
                },
                SalesPersonId = salesOrder.SalesPerson,
                SalesPerson = salesOrder.SalesPerson,
                Currency = "INR",
                ExchangeRate = 1m,
                QuotationId = salesOrder.QuotationId,
                QuotationNumber = salesOrder.QuotationNumber,
                SalesOrderId = salesOrder.Id,
                SalesOrderNumber = salesOrder.SalesOrderNumber,
                PaymentTerms = string.IsNullOrWhiteSpace(salesOrder.PaymentTerms) ? "Net 30" : salesOrder.PaymentTerms,
                DeliveryTerms = string.IsNullOrWhiteSpace(salesOrder.DeliveryTerms) ? "Standard Delivery" : salesOrder.DeliveryTerms,
                CustomerNotes = salesOrder.Notes ?? string.Empty,
                InternalNotes = $"Generated from Sales Order {salesOrder.SalesOrderNumber}",
                Items = items,
                Status = ProformaInvoiceStatuses.Submitted
            };

            return await CreateAsync(request, actingUser, cancellationToken);
        }


        public async Task<ProformaInvoiceDto?> DuplicateAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var source = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            if (source is null)
            {
                return null;
            }

            var create = new ProformaInvoiceCreateRequestDto
            {
                InvoiceDate = DateHelper.FormatDate(DateOnly.FromDateTime(DateTime.UtcNow)),
                ValidUntil = DateHelper.FormatDate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30)),
                Customer = new ProformaInvoiceCustomerDto
                {
                    CustomerId = source.CustomerId,
                    CustomerName = source.CustomerName,
                    ContactPerson = source.ContactPerson,
                    BillingAddress = source.BillingAddress,
                    ShippingAddress = source.ShippingAddress
                },
                SalesPersonId = source.SalesPersonUserId.ToString(),
                SalesPerson = source.SalesPerson,
                Currency = source.Currency,
                ExchangeRate = source.ExchangeRate,
                QuotationId = source.QuotationId,
                QuotationNumber = source.QuotationNumber,
                SalesOrderId = source.SalesOrderId,
                SalesOrderNumber = source.SalesOrderNumber,
                PaymentTerms = source.PaymentTerms,
                DeliveryTerms = source.DeliveryTerms,
                CustomerNotes = source.CustomerNotes,
                InternalNotes = source.InternalNotes,
                Items = source.Items.OrderBy(i => i.SortOrder).Select(i => new ProformaInvoiceItemDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Rate = i.Rate,
                    Discount = i.Discount,
                    Gst = i.Gst,
                    TaxAmount = i.TaxAmount,
                    Amount = i.Amount
                }).ToList(),
                Status = ProformaInvoiceStatuses.Draft
            };

            return await CreateAsync(create, actingUser, cancellationToken);
        }

        public async Task<ProformaInvoiceDto?> UpdateStatusAsync(
            int id,
            ProformaInvoiceStatusUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var target = ProformaInvoiceStatusRules.Normalize(request.Status)
                ?? throw new InvalidOperationException($"Unknown status '{request.Status}'.");

            if (!ProformaInvoiceStatusRules.CanTransition(entity.Status, target))
            {
                throw new InvalidOperationException(
                    $"Cannot transition proforma from '{entity.Status}' to '{target}'.");
            }

            if (entity.Status.Equals(target, StringComparison.Ordinal))
            {
                return ProformaInvoiceMapper.ToDto(entity);
            }

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = target;
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewStatusHistory(old, target, request.Remarks?.Trim() ?? string.Empty, actingUser, now));
            await _repo.SaveChangesAsync(cancellationToken);

            return ProformaInvoiceMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<ProformaInvoiceDto?> ApplyApprovalAsync(
            int id,
            ProformaInvoiceApprovalRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var kind = (request.Kind ?? request.Decision ?? string.Empty).Trim();
            var decision = kind.ToLowerInvariant() switch
            {
                "approve" or "approved" => ProformaApprovalDecisions.Approve,
                "reject" or "rejected" => ProformaApprovalDecisions.Reject,
                "return" or "returned" => ProformaApprovalDecisions.Return,
                "submit" or "submitted" => ProformaApprovalDecisions.Submit,
                _ => throw new InvalidOperationException($"Unknown approval action '{kind}'.")
            };

            var targetStatus = decision switch
            {
                ProformaApprovalDecisions.Submit => ProformaInvoiceStatuses.PendingFinanceApproval,
                ProformaApprovalDecisions.Approve => ProformaInvoiceStatuses.Approved,
                ProformaApprovalDecisions.Reject => ProformaInvoiceStatuses.Rejected,
                ProformaApprovalDecisions.Return => ProformaInvoiceStatuses.Returned,
                _ => throw new InvalidOperationException("Unsupported approval decision.")
            };

            // Submit from Draft goes Draft -> Submitted first, then to Pending Finance when manager submits to finance.
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (decision == ProformaApprovalDecisions.Submit
                && entity.Status == ProformaInvoiceStatuses.Draft)
            {
                targetStatus = ProformaInvoiceStatuses.Submitted;
            }
            else if (decision == ProformaApprovalDecisions.Submit
                && entity.Status == ProformaInvoiceStatuses.Submitted)
            {
                targetStatus = ProformaInvoiceStatuses.PendingFinanceApproval;
            }

            var level = string.IsNullOrWhiteSpace(request.ApprovalLevel)
                ? (entity.Status == ProformaInvoiceStatuses.PendingFinanceApproval
                    || targetStatus == ProformaInvoiceStatuses.Approved
                    ? ProformaApprovalLevels.Finance
                    : ProformaApprovalLevels.Manager)
                : request.ApprovalLevel.Trim();

            var updated = await UpdateStatusAsync(
                id,
                new ProformaInvoiceStatusUpdateRequestDto
                {
                    Status = targetStatus,
                    Remarks = request.Remarks
                },
                actingUser,
                cancellationToken);

            if (updated is null)
            {
                return null;
            }

            entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return updated;
            }

            entity.ApprovalHistory.Add(NewApproval(
                level,
                decision,
                request.Remarks?.Trim() ?? string.Empty,
                actingUser,
                DateTimeOffset.UtcNow));
            await _repo.SaveChangesAsync(cancellationToken);

            return ProformaInvoiceMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<ProformaInvoiceDto?> ConvertAsync(
            int id,
            ProformaInvoiceConvertRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (entity.Status != ProformaInvoiceStatuses.Accepted
                && !ProformaInvoiceStatusRules.CanTransition(entity.Status, ProformaInvoiceStatuses.Converted))
            {
                // Allow convert from Accepted primarily; also if already allowed by rules.
            }

            if (!ProformaInvoiceStatusRules.CanTransition(entity.Status, ProformaInvoiceStatuses.Converted)
                && entity.Status != ProformaInvoiceStatuses.Accepted)
            {
                throw new InvalidOperationException(
                    $"Cannot convert proforma from '{entity.Status}'. Accept it first.");
            }

            // Force Accepted -> Converted if currently Accepted.
            if (entity.Status == ProformaInvoiceStatuses.Accepted
                || ProformaInvoiceStatusRules.CanTransition(entity.Status, ProformaInvoiceStatuses.Converted))
            {
                var now = DateTimeOffset.UtcNow;
                var old = entity.Status;
                if (old != ProformaInvoiceStatuses.Accepted
                    && old != ProformaInvoiceStatuses.Converted
                    && ProformaInvoiceStatusRules.CanTransition(old, ProformaInvoiceStatuses.Accepted))
                {
                    entity.StatusHistory.Add(NewStatusHistory(old, ProformaInvoiceStatuses.Accepted, "Auto-accepted before convert", actingUser, now));
                    old = ProformaInvoiceStatuses.Accepted;
                    entity.Status = ProformaInvoiceStatuses.Accepted;
                }

                entity.Status = ProformaInvoiceStatuses.Converted;
                entity.ConvertedInvoiceNumber = $"SI-{DateTime.UtcNow.Year}-{entity.Id:D5}";
                entity.ConvertedOn = now;
                entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
                entity.UpdatedBy = actingUser;
                entity.UpdatedDate = now;
                entity.StatusHistory.Add(NewStatusHistory(
                    old,
                    ProformaInvoiceStatuses.Converted,
                    request.Remarks?.Trim() ?? "Converted to sales invoice",
                    actingUser,
                    now));
                await _repo.SaveChangesAsync(cancellationToken);
            }

            return ProformaInvoiceMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<IReadOnlyList<ProformaInvoiceStatusHistoryDto>?> GetStatusHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity is null ? null : ProformaInvoiceMapper.ToDto(entity).StatusHistory;
        }

        public async Task<IReadOnlyList<ProformaInvoiceApprovalHistoryDto>?> GetApprovalHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity is null ? null : ProformaInvoiceMapper.ToDto(entity).ApprovalHistory;
        }

        public async Task<ProformaDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            var rows = await _repo.Query().AsNoTracking().ToListAsync(cancellationToken);
            return new ProformaDashboardDto
            {
                TotalCount = rows.Count,
                DraftCount = rows.Count(x => x.Status == ProformaInvoiceStatuses.Draft),
                PendingApprovalCount = rows.Count(x =>
                    x.Status is ProformaInvoiceStatuses.Submitted
                        or ProformaInvoiceStatuses.PendingFinanceApproval),
                ApprovedCount = rows.Count(x => x.Status == ProformaInvoiceStatuses.Approved),
                SentCount = rows.Count(x => x.Status == ProformaInvoiceStatuses.Sent),
                ConvertedCount = rows.Count(x => x.Status == ProformaInvoiceStatuses.Converted),
                TotalValue = rows.Sum(x => x.GrandTotal),
                Recent = rows.OrderByDescending(x => x.UpdatedDate).Take(10)
                    .Select(ProformaInvoiceMapper.ToListItem).ToList()
            };
        }

        public async Task<ProformaReportResultDto> GetReportsAsync(
            ProformaInvoiceListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await GetAllAsync(query, cancellationToken);
            return new ProformaReportResultDto
            {
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O"),
                RowCount = rows.Count,
                Rows = rows.ToList()
            };
        }

        public async Task<ProformaExportMetadataDto> ExportReportsAsync(
            ProformaExportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var report = await GetReportsAsync(new ProformaInvoiceListQueryDto
            {
                Status = request.Status,
                DateFrom = request.DateFrom,
                DateTo = request.DateTo
            }, cancellationToken);

            return new ProformaExportMetadataDto
            {
                FileName = $"proforma-report-{DateTime.UtcNow:yyyyMMddHHmmss}.{(request.Format?.ToLowerInvariant() == "xlsx" ? "xlsx" : "csv")}",
                ContentType = request.Format?.ToLowerInvariant() == "xlsx"
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv",
                Message = $"Export prepared for {report.RowCount} row(s) (placeholder).",
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O")
            };
        }

        public async Task<ProformaPdfResultDto?> GeneratePdfAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, false, false, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            return new ProformaPdfResultDto
            {
                FileName = $"{entity.PiNumber}.pdf",
                Message = "PDF generation placeholder — document engine not wired yet.",
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O")
            };
        }

        public async Task<ProformaEmailResultDto?> SendEmailAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, false, false, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            return new ProformaEmailResultDto
            {
                Success = true,
                Message = $"Email placeholder queued for {entity.PiNumber}."
            };
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "proforma-invoices.view",
                "proforma-invoices.create",
                "proforma-invoices.edit",
                "proforma-invoices.delete",
                "proforma-invoices.review",
                "proforma-invoices.dashboard.view",
                "proforma-invoices.report.view"
            ]);

        public async Task<IReadOnlyList<ProformaLookupCustomerDto>> LookupCustomersAsync(
            CancellationToken cancellationToken = default)
        {
            var ledgerCustomers = await _context.CustomerLedgerEntries
                .AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.CustomerName))
                .Select(x => new { CustomerId = x.CustomerId > 0 ? x.CustomerId.ToString() : x.CustomerName, CustomerName = x.CustomerName, ContactPerson = "" })
                .Distinct()
                .ToListAsync(cancellationToken);

            var piCustomers = await _repo.Query().AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.CustomerName))
                .GroupBy(x => new { x.CustomerId, x.CustomerName, x.ContactPerson })
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            var combined = ledgerCustomers
                .Concat(piCustomers.Select(x => new { CustomerId = string.IsNullOrWhiteSpace(x.CustomerId) ? x.CustomerName : x.CustomerId, CustomerName = x.CustomerName, ContactPerson = x.ContactPerson }))
                .GroupBy(x => x.CustomerName.Trim().ToLower())
                .Select(g => g.First())
                .OrderBy(x => x.CustomerName)
                .Take(200)
                .ToList();

            return combined.Select(x => new ProformaLookupCustomerDto
            {
                Id = x.CustomerId,
                Name = x.CustomerName,
                ContactPerson = x.ContactPerson
            }).ToList();
        }

        public async Task<IReadOnlyList<ProformaLookupSalesOrderDto>> LookupSalesOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            var orders = await _repo.ListSalesOrdersForLookupAsync(cancellationToken);
            return orders.Select(o => new ProformaLookupSalesOrderDto
            {
                Id = o.Id,
                SalesOrderNumber = o.SalesOrderNumber,
                CustomerName = o.CustomerName,
                Status = o.Status
            }).ToList();
        }

        public async Task<IReadOnlyList<ProformaLookupQuotationDto>> LookupQuotationsAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.Query().AsNoTracking()
                .Where(x => x.QuotationId != null && x.QuotationNumber != "")
                .GroupBy(x => new { x.QuotationId, x.QuotationNumber, x.CustomerName })
                .Select(g => g.Key)
                .OrderByDescending(x => x.QuotationId)
                .Take(100)
                .ToListAsync(cancellationToken);

            return rows.Select(x => new ProformaLookupQuotationDto
            {
                Id = x.QuotationId!.Value,
                QuotationNumber = x.QuotationNumber,
                CustomerName = x.CustomerName
            }).ToList();
        }

        private static IQueryable<ProformaInvoice> ApplyFilters(
            IQueryable<ProformaInvoice> q,
            ProformaInvoiceListQueryDto? query)
        {
            if (query is null)
            {
                return q;
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = ProformaInvoiceStatusRules.Normalize(query.Status) ?? query.Status.Trim();
                q = q.Where(x => x.Status == status);
            }

            var from = ProformaInvoiceMapper.ParseOptionalDate(query.DateFrom);
            if (from is not null)
            {
                q = q.Where(x => x.InvoiceDate >= from);
            }

            var to = ProformaInvoiceMapper.ParseOptionalDate(query.DateTo);
            if (to is not null)
            {
                q = q.Where(x => x.InvoiceDate <= to);
            }

            if (!string.IsNullOrWhiteSpace(query.Customer))
            {
                var c = query.Customer.Trim().ToLower();
                q = q.Where(x => x.CustomerName.ToLower().Contains(c));
            }

            if (!string.IsNullOrWhiteSpace(query.SalesPerson))
            {
                var s = query.SalesPerson.Trim().ToLower();
                q = q.Where(x => x.SalesPerson.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.PiNumber.ToLower().Contains(term)
                    || x.CustomerName.ToLower().Contains(term)
                    || x.QuotationNumber.ToLower().Contains(term)
                    || x.SalesOrderNumber.ToLower().Contains(term));
            }

            return q;
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
                    // Graceful: keep the id/number without failing when SO missing.
                    return (id, salesOrderNumber?.Trim() ?? string.Empty);
                }

                return (so.Id, so.SalesOrderNumber);
            }

            return (null, salesOrderNumber?.Trim() ?? string.Empty);
        }

        private static void ValidateRequest(
            ProformaInvoiceCustomerDto customer,
            IList<ProformaInvoiceItemDto> items,
            string invoiceDate,
            string validUntil,
            string currency)
        {
            if (customer is null || string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                throw new InvalidOperationException("Customer name is required.");
            }

            if (string.IsNullOrWhiteSpace(invoiceDate))
            {
                throw new InvalidOperationException("Invoice date is required.");
            }

            if (string.IsNullOrWhiteSpace(validUntil))
            {
                throw new InvalidOperationException("Valid until date is required.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new InvalidOperationException("Currency is required.");
            }

            if (items is null || items.Count == 0)
            {
                throw new InvalidOperationException("At least one line item is required.");
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemName))
                {
                    throw new InvalidOperationException("Each line item requires an item name.");
                }

                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException("Line item quantity must be greater than zero.");
                }
            }
        }

        private static List<ProformaInvoiceItemDto> PrepareItems(IEnumerable<ProformaInvoiceItemDto> items) =>
            items.Select(item =>
            {
                var tax = ProformaInvoiceCalculator.CalcLineTaxAmount(
                    item.Quantity, item.Rate, item.Discount, item.Gst);
                var amount = ProformaInvoiceCalculator.CalcLineAmount(
                    item.Quantity, item.Rate, item.Discount, item.Gst);
                return new ProformaInvoiceItemDto
                {
                    Id = item.Id,
                    ItemName = item.ItemName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    TaxAmount = tax,
                    Amount = amount
                };
            }).ToList();

        private static List<ProformaInvoiceItem> MapItems(IReadOnlyList<ProformaInvoiceItemDto> items)
        {
            var list = new List<ProformaInvoiceItem>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                list.Add(new ProformaInvoiceItem
                {
                    LineKey = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Trim(),
                    SortOrder = i,
                    ItemName = item.ItemName.Trim(),
                    Description = item.Description?.Trim() ?? string.Empty,
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Nos" : item.Unit.Trim(),
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    TaxAmount = item.TaxAmount,
                    Amount = item.Amount
                });
            }

            return list;
        }

        private static int ParseUserId(string? salesPersonId, string fallback)
        {
            if (int.TryParse(salesPersonId, out var id) && id > 0)
            {
                return id;
            }

            if (int.TryParse(fallback.Replace("user:", "", StringComparison.OrdinalIgnoreCase), out var fromActor)
                && fromActor > 0)
            {
                return fromActor;
            }

            return 1;
        }

        private static ProformaInvoiceStatusHistory NewStatusHistory(
            string? oldStatus,
            string newStatus,
            string remarks,
            string changedBy,
            DateTimeOffset changedOn) => new()
        {
            EntryKey = Guid.NewGuid().ToString("N"),
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Remarks = remarks,
            ChangedBy = changedBy,
            ChangedOn = changedOn
        };

        private static ProformaInvoiceApprovalHistory NewApproval(
            string level,
            string decision,
            string remarks,
            string approvedBy,
            DateTimeOffset approvedOn) => new()
        {
            EntryKey = Guid.NewGuid().ToString("N"),
            ApprovalLevel = level,
            Decision = decision,
            Remarks = remarks,
            ApprovedBy = approvedBy,
            ApprovedOn = approvedOn
        };
    }
}
