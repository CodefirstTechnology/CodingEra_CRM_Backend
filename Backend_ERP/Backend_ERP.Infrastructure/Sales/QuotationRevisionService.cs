using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Quotations;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class QuotationRevisionService : IQuotationRevisionService
    {
        private readonly ERPDbContext _dbContext;

        public QuotationRevisionService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Quotation> CreateRevisionAsync(
            int quotationId,
            string? reason,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existing = await _dbContext.Quotations
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken)
                ?? throw new InvalidOperationException($"Quotation with ID {quotationId} not found.");

            if (!existing.IsCurrentRevision)
            {
                throw new InvalidOperationException($"Quotation {existing.QuotationNumber} is not the current active revision and cannot be revised.");
            }

            if (existing.Status == QuotationStatuses.Revised)
            {
                throw new InvalidOperationException($"Quotation {existing.QuotationNumber} is already in 'Revised' state.");
            }

            if (existing.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException($"Quotation {existing.QuotationNumber} has already been converted to Sales Order {existing.ConvertedSalesOrderNumber} and cannot be revised.");
            }

            // Lock predecessor revision
            existing.IsCurrentRevision = false;
            existing.Status = QuotationStatuses.Revised;
            existing.UpdatedBy = actingUser;
            existing.UpdatedDate = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                existing.Notes = string.IsNullOrWhiteSpace(existing.Notes)
                    ? $"[Revision Reason]: {reason}"
                    : $"{existing.Notes}\n[Revision Reason]: {reason}";
            }

            var nextRevisionNumber = existing.RevisionNumber + 1;
            var rootId = existing.RootQuotationId ?? existing.Id;

            // Generate clean revision number (e.g., QT-2026-001 -> QT-2026-001-R1)
            var baseNumber = Regex.Replace(existing.QuotationNumber, @"-R\d+$", string.Empty, RegexOptions.IgnoreCase);
            var newQuotationNumber = $"{baseNumber}-R{nextRevisionNumber}";

            var revision = new Quotation
            {
                TenantId = existing.TenantId,
                QuotationNumber = newQuotationNumber,
                ParentQuotationId = existing.Id,
                RootQuotationId = rootId,
                RevisionNumber = nextRevisionNumber,
                IsCurrentRevision = true,
                CustomerId = existing.CustomerId,
                CustomerName = existing.CustomerName,
                CustomerCategory = existing.CustomerCategory,
                ContactPerson = existing.ContactPerson,
                CustomerEmail = existing.CustomerEmail,
                CustomerPhone = existing.CustomerPhone,
                BillingAddress = existing.BillingAddress,
                ShippingAddress = existing.ShippingAddress,
                SalesPerson = existing.SalesPerson,
                SalesPersonUserId = existing.SalesPersonUserId,
                Status = QuotationStatuses.Draft,
                QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ValidUntil = existing.ValidUntil,
                Currency = existing.Currency,
                ExchangeRate = existing.ExchangeRate,
                PaymentTerms = existing.PaymentTerms,
                DeliveryTerms = existing.DeliveryTerms,
                FreightAmount = existing.FreightAmount,
                PackagingAmount = existing.PackagingAmount,
                RoundOff = existing.RoundOff,
                Notes = string.IsNullOrWhiteSpace(reason) 
                    ? existing.Notes 
                    : $"[Revised from {existing.QuotationNumber}]: {reason}\n{existing.Notes}",
                CreatedBy = actingUser,
                CreatedDate = DateTimeOffset.UtcNow,
                UpdatedBy = actingUser,
                UpdatedDate = DateTimeOffset.UtcNow,
                DiscountApprovalStatus = "None",
                ApprovalToken = null,
                ConvertedSalesOrderId = null,
                ConvertedSalesOrderNumber = null,
                ConvertedOn = null,
                ClientPoNumber = null,
                ClientAcceptedAt = null,
                ClientPoAttachmentUrl = null
            };

            // Deep-clone all line items preserving physical linkages & frozen commercial snapshots
            foreach (var item in existing.Items.OrderBy(i => i.LineNumber))
            {
                revision.Items.Add(new QuotationItem
                {
                    TenantId = existing.TenantId,
                    ItemType = item.ItemType,
                    ItemId = item.ItemId,
                    ProductId = item.ProductId,
                    WarehouseId = item.WarehouseId,
                    BaseStockUom = item.BaseStockUom,
                    LineNumber = item.LineNumber,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount = item.DiscountAmount,
                    TaxPercent = item.TaxPercent,
                    TaxAmount = item.TaxAmount,
                    LineTotal = item.LineTotal,
                    SortOrder = item.SortOrder,
                    ResolvedPriceListId = item.ResolvedPriceListId,
                    PriceListNumberSnapshot = item.PriceListNumberSnapshot,
                    ResolutionStageSnapshot = item.ResolutionStageSnapshot,
                    OriginalSellingRate = item.OriginalSellingRate,
                    FloorPriceSnapshot = item.FloorPriceSnapshot,
                    MaxDiscountPctSnapshot = item.MaxDiscountPctSnapshot,
                    TaxSlabPercentSnapshot = item.TaxSlabPercentSnapshot,
                    SnapshotTimestampUtc = item.SnapshotTimestampUtc,
                    IsCommercialDeviation = item.IsCommercialDeviation
                });
            }

            revision.CalculateTotals();

            await _dbContext.Quotations.AddAsync(revision, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return revision;
        }
    }
}
