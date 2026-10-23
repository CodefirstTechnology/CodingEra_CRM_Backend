using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Approvals;
using ERP.Application.Sales.Dtos;
using ERP.Application.Sales.Quotations;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class QuotationConversionService : IQuotationConversionService
    {
        private readonly ERPDbContext _dbContext;
        private readonly ICommercialComplianceService _complianceService;
        private readonly IInventoryAllocationService _allocationService;

        public QuotationConversionService(
            ERPDbContext dbContext,
            ICommercialComplianceService complianceService,
            IInventoryAllocationService allocationService)
        {
            _dbContext = dbContext;
            _complianceService = complianceService;
            _allocationService = allocationService;
        }

        public async Task<SalesOrderDto> ConvertQuotationToSalesOrderAsync(
            int quotationId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // 1. Pessimistic Row Lock (FOR UPDATE)
            var quote = await _dbContext.Quotations
                .FromSqlRaw(@"SELECT *, xmin FROM quotations WHERE ""Id"" = {0} AND ""IsDeleted"" = false FOR UPDATE", quotationId)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Quotation with ID {quotationId} not found.");

            // 2. State & Lineage Validations
            if (!quote.IsCurrentRevision)
            {
                throw new InvalidOperationException(
                    $"Quotation '{quote.QuotationNumber}' is not the current active revision and cannot be converted to a Sales Order.");
            }

            if (!string.Equals(quote.Status, QuotationStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Quotation must be in 'Approved' (Client Accepted) status to convert to a Sales Order. Current status: '{quote.Status}'.");
            }

            if (quote.ConvertedSalesOrderId.HasValue && quote.ConvertedSalesOrderId.Value > 0)
            {
                throw new InvalidOperationException(
                    $"Quotation '{quote.QuotationNumber}' has already been converted to Sales Order '{quote.ConvertedSalesOrderNumber ?? quote.ConvertedSalesOrderId.Value.ToString()}'.");
            }

            // 3. Cryptographic Tamper-Proof HMAC-SHA256 Verification
            if (!string.IsNullOrWhiteSpace(quote.ApprovalToken))
            {
                var isValid = _complianceService.VerifyApprovalToken(quote, quote.ApprovalToken);
                if (!isValid)
                {
                    throw new InvalidOperationException(
                        "Commercial compliance violation: Quotation pricing or item configuration has been tampered with after sign-off. HMAC-SHA256 signature token is invalid.");
                }
            }

            // 4. Generate Sales Order Number
            var year = DateTime.UtcNow.Year;
            var seq = await _dbContext.SalesOrderDocumentSequences
                .FirstOrDefaultAsync(x => x.Year == year, cancellationToken);

            if (seq is null)
            {
                seq = new SalesOrderDocumentSequence { Year = year, LastSequence = 0 };
                _dbContext.SalesOrderDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            var soNumber = $"SO-{year}-{seq.LastSequence:D4}";

            var now = DateTimeOffset.UtcNow;

            var order = new SalesOrder
            {
                TenantId = quote.TenantId,
                SalesOrderNumber = soNumber,
                QuotationId = quote.Id,
                QuotationNumber = quote.QuotationNumber,
                ApprovalToken = quote.ApprovalToken,
                SourceType = SalesOrderSourceTypes.Quotation,
                CustomerName = quote.CustomerName,
                ContactPerson = quote.ContactPerson,
                BillingAddress = quote.BillingAddress,
                ShippingAddress = quote.ShippingAddress,
                CustomerEmail = quote.CustomerEmail,
                CustomerPhone = quote.CustomerPhone,
                SalesPerson = quote.SalesPerson,
                SalesPersonUserId = quote.SalesPersonUserId,
                Notes = quote.Notes,
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PaymentTerms = quote.PaymentTerms,
                DeliveryTerms = quote.DeliveryTerms,
                Subtotal = quote.Subtotal,
                DiscountTotal = quote.DiscountTotal,
                GstTotal = quote.TaxTotal,
                GrandTotal = quote.GrandTotal,
                Status = SalesOrderStatuses.Draft,
                Remarks = $"Converted from Quotation {quote.QuotationNumber} (Rev {quote.RevisionNumber})",
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now
            };

            // 5. Promote Zero-Recalculation Commercial Snapshots and Physical Linkages
            int sortIndex = 1;
            foreach (var item in quote.Items.OrderBy(i => i.SortOrder))
            {
                order.Items.Add(new SalesOrderItem
                {
                    TenantId = quote.TenantId,
                    LineKey = Guid.NewGuid().ToString("N"),
                    SortIndex = sortIndex++,
                    ItemType = item.ItemType,
                    ItemId = item.ItemId,
                    WarehouseId = item.WarehouseId ?? 1,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    DispatchedQuantity = 0m,
                    ProformaInvoicedQuantity = 0m,
                    Unit = item.Unit,
                    BaseStockUom = string.IsNullOrWhiteSpace(item.BaseStockUom) ? item.Unit : item.BaseStockUom,
                    Rate = item.UnitPrice,
                    Discount = item.DiscountPercent,
                    Gst = item.TaxPercent,
                    Amount = item.LineTotal,
                    ResolvedPriceListId = item.ResolvedPriceListId,
                    PriceListNumberSnapshot = item.PriceListNumberSnapshot,
                    OriginalSellingRate = item.OriginalSellingRate,
                    FloorPriceSnapshot = item.FloorPriceSnapshot,
                    MaxDiscountPctSnapshot = item.MaxDiscountPctSnapshot,
                    TaxSlabPercentSnapshot = item.TaxSlabPercentSnapshot
                });
            }

            order.StatusHistory.Add(new SalesOrderStatusHistory
            {
                EntryKey = Guid.NewGuid().ToString("N"),
                Status = SalesOrderStatuses.Draft,
                Date = now,
                User = actingUser,
                Label = "Created from Quotation",
                Remarks = $"Converted from Quotation {quote.QuotationNumber}"
            });

            await _dbContext.SalesOrders.AddAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 6. Soft Reserve Physical Inventory in Warehouse
            await _allocationService.SoftReserveQuotationStockAsync(quote, cancellationToken);

            // 7. Update Quotation Status and Conversion Metadata
            quote.ConvertedSalesOrderId = order.Id;
            quote.ConvertedSalesOrderNumber = order.SalesOrderNumber;
            quote.ConvertedOn = now;
            quote.Status = QuotationStatuses.ConvertedToSO;
            quote.UpdatedBy = actingUser;
            quote.UpdatedDate = now;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return SalesOrderMapper.ToDto(order);
        }
    }
}
