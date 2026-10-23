using ERP.Shared.Models;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using ERP.Application.Sales.Quotations;
using ERP.Application.Sales.Approvals;
using ERP.Domain.Procurement;

namespace ERP.Infrastructure.Sales
{
    public class QuotationService : IQuotationService
    {
        private readonly ERPDbContext _db;
        private readonly IDiscountApprovalNumberingService _numbering;
        private readonly IQuotationRevisionService _revisionService;
        private readonly ERP.Application.Sales.Approvals.ICommercialComplianceService _complianceService;
        private readonly IInventoryAllocationService _allocationService;

        public QuotationService(
            ERPDbContext db,
            IDiscountApprovalNumberingService numbering,
            IQuotationRevisionService revisionService,
            ERP.Application.Sales.Approvals.ICommercialComplianceService complianceService,
            IInventoryAllocationService allocationService)
        {
            _db = db;
            _numbering = numbering;
            _revisionService = revisionService;
            _complianceService = complianceService;
            _allocationService = allocationService;
        }

        public async Task<PagedResult<QuotationDto>> GetPagedAsync(
            QuotationListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.Quotations
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => !x.IsDeleted);

            if (query.CurrentOnly)
            {
                q = q.Where(x => x.IsCurrentRevision);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                q = q.Where(x => x.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.Customer))
            {
                q = q.Where(x => EF.Functions.ILike(x.CustomerName, $"%{query.Customer}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = $"%{query.Search}%";
                q = q.Where(x =>
                    EF.Functions.ILike(x.QuotationNumber, s) ||
                    EF.Functions.ILike(x.CustomerName, s) ||
                    (x.ClientPoNumber != null && EF.Functions.ILike(x.ClientPoNumber, s)));
            }

            if (query.DateFrom.HasValue)
            {
                q = q.Where(x => x.QuotationDate >= query.DateFrom.Value);
            }

            if (query.DateTo.HasValue)
            {
                q = q.Where(x => x.QuotationDate <= query.DateTo.Value);
            }

            var total = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 200);

            var items = await q
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<QuotationDto>.Create(
                items.Select(MapToDto).ToList(),
                total,
                page,
                pageSize);
        }

        public async Task<QuotationDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<QuotationDto> CreateAsync(
            QuotationCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var number = await GetNextNumberAsync(cancellationToken);

            var entity = new Quotation
            {
                QuotationNumber = number,
                RevisionNumber = 1,
                IsCurrentRevision = true,
                CustomerId = request.CustomerId ?? string.Empty,
                CustomerName = request.CustomerName,
                ContactPerson = request.ContactPerson ?? string.Empty,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                BillingAddress = request.BillingAddress ?? string.Empty,
                ShippingAddress = request.ShippingAddress ?? string.Empty,
                SalesPerson = request.SalesPerson ?? actingUser,
                Status = QuotationStatuses.Draft,
                QuotationDate = request.QuotationDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                ValidUntil = request.ValidUntil ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                Currency = request.Currency ?? "INR",
                ExchangeRate = request.ExchangeRate ?? 1.0m,
                FreightAmount = request.FreightAmount ?? 0m,
                PackagingAmount = request.PackagingAmount ?? 0m,
                RoundOff = request.RoundOff ?? 0m,
                PaymentTerms = request.PaymentTerms ?? "Net 30",
                DeliveryTerms = request.DeliveryTerms ?? "Ex-Works",
                Notes = request.Notes ?? string.Empty,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                IsDeleted = false
            };

            var candidateIds = request.Items
                .Select(x => x.ProductId ?? x.ItemId)
                .Where(x => x.HasValue && x.Value > 0)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            var candidateCodes = request.Items
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemCode))
                .Select(x => x.ItemCode!.Trim())
                .Distinct()
                .ToList();

            var matchingGoods = await _db.FinishedGoods
                .Where(fg => !fg.IsDeleted && (candidateIds.Contains(fg.Id) || (candidateCodes.Count > 0 && candidateCodes.Contains(fg.ProductCode))))
                .ToListAsync(cancellationToken);

            var goodsById = matchingGoods.ToDictionary(x => x.Id);
            var goodsByCode = matchingGoods.GroupBy(x => x.ProductCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            HydrateItems(entity, request.Items, goodsById, goodsByCode);
            CalculateTotals(entity);

            var maxDiscount = entity.Items.Count > 0 ? entity.Items.Max(i => i.DiscountPercent) : 0m;
            if (maxDiscount > 5.00m)
            {
                entity.Status = QuotationStatuses.PendingDiscountApproval;
                entity.DiscountApprovalStatus = DiscountApprovalStatuses.Pending;
            }
            else
            {
                entity.Status = QuotationStatuses.Draft;
                entity.DiscountApprovalStatus = "None";
            }

            _db.Quotations.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            if (maxDiscount > 5.00m)
            {
                var approvalNumber = await _numbering.GenerateNextApprovalNumberAsync(cancellationToken);
                var approval = new DiscountApproval
                {
                    ApprovalNumber = approvalNumber,
                    RequestDate = entity.QuotationDate,
                    SourceType = DiscountApprovalSourceTypes.Quotation,
                    QuotationId = entity.Id,
                    QuotationNumber = entity.QuotationNumber,
                    CustomerName = entity.CustomerName,
                    CustomerCategory = PriceListCustomerCategories.Standard,
                    SalesPersonUserId = 1,
                    RequestedDiscountPercentage = maxDiscount,
                    RequestedAmount = entity.DiscountTotal,
                    ApprovalLevel = DiscountApprovalLevels.Manager,
                    Priority = DiscountApprovalPriorities.Normal,
                    Status = DiscountApprovalStatuses.Pending,
                    Reason = $"Quotation line discount of {maxDiscount:F2}% exceeds 5.00% threshold.",
                    Remarks = $"Auto-generated from Quotation {entity.QuotationNumber}",
                    CreatedBy = actingUser,
                    CreatedDate = now,
                    UpdatedBy = actingUser,
                    UpdatedDate = now,
                    IsDeleted = false
                };

                approval.History.Add(new DiscountApprovalHistory
                {
                    Action = DiscountApprovalHistoryActions.Created,
                    OldStatus = string.Empty,
                    NewStatus = DiscountApprovalStatuses.Pending,
                    Remarks = $"Discount approval request auto-created from Quotation {entity.QuotationNumber}",
                    PerformedBy = actingUser,
                    PerformedOn = now
                });

                _db.DiscountApprovals.Add(approval);
                await _db.SaveChangesAsync(cancellationToken);

                entity.DiscountApprovalId = approval.Id;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(entity);
        }

        public async Task<QuotationDto?> UpdateAsync(
            int id,
            QuotationUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            if (entity.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Converted quotation cannot be modified.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.CustomerName = request.CustomerName;
            entity.ContactPerson = request.ContactPerson ?? string.Empty;
            entity.CustomerEmail = request.CustomerEmail;
            entity.CustomerPhone = request.CustomerPhone;
            entity.BillingAddress = request.BillingAddress ?? string.Empty;
            entity.ShippingAddress = request.ShippingAddress ?? string.Empty;
            entity.SalesPerson = request.SalesPerson ?? entity.SalesPerson;
            if (request.QuotationDate.HasValue) entity.QuotationDate = request.QuotationDate.Value;
            if (request.ValidUntil.HasValue) entity.ValidUntil = request.ValidUntil.Value;
            entity.Currency = request.Currency ?? entity.Currency;
            entity.ExchangeRate = request.ExchangeRate ?? entity.ExchangeRate;
            if (request.FreightAmount.HasValue) entity.FreightAmount = request.FreightAmount.Value;
            if (request.PackagingAmount.HasValue) entity.PackagingAmount = request.PackagingAmount.Value;
            if (request.RoundOff.HasValue) entity.RoundOff = request.RoundOff.Value;
            entity.PaymentTerms = request.PaymentTerms ?? entity.PaymentTerms;
            entity.DeliveryTerms = request.DeliveryTerms ?? entity.DeliveryTerms;
            entity.Notes = request.Notes ?? entity.Notes;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            _db.QuotationItems.RemoveRange(entity.Items);
            entity.Items.Clear();

            var candidateIds = request.Items
                .Select(x => x.ProductId ?? x.ItemId)
                .Where(x => x.HasValue && x.Value > 0)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            var candidateCodes = request.Items
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemCode))
                .Select(x => x.ItemCode!.Trim())
                .Distinct()
                .ToList();

            var matchingGoods = await _db.FinishedGoods
                .Where(fg => !fg.IsDeleted && (candidateIds.Contains(fg.Id) || (candidateCodes.Count > 0 && candidateCodes.Contains(fg.ProductCode))))
                .ToListAsync(cancellationToken);

            var goodsById = matchingGoods.ToDictionary(x => x.Id);
            var goodsByCode = matchingGoods.GroupBy(x => x.ProductCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            HydrateItems(entity, request.Items, goodsById, goodsByCode);
            CalculateTotals(entity);

            var maxDiscount = entity.Items.Count > 0 ? entity.Items.Max(i => i.DiscountPercent) : 0m;
            if (maxDiscount > 5.00m)
            {
                entity.Status = QuotationStatuses.PendingDiscountApproval;
                entity.DiscountApprovalStatus = DiscountApprovalStatuses.Pending;

                DiscountApproval? da = null;
                if (entity.DiscountApprovalId.HasValue)
                {
                    da = await _db.DiscountApprovals
                        .Include(x => x.History)
                        .FirstOrDefaultAsync(x => x.Id == entity.DiscountApprovalId.Value && !x.IsDeleted, cancellationToken);
                }

                if (da != null && (da.Status == DiscountApprovalStatuses.Pending || da.Status == DiscountApprovalStatuses.UnderReview))
                {
                    da.RequestedDiscountPercentage = maxDiscount;
                    da.RequestedAmount = entity.DiscountTotal;
                    da.UpdatedBy = actingUser;
                    da.UpdatedDate = now;
                    da.History.Add(new DiscountApprovalHistory
                    {
                        Action = DiscountApprovalHistoryActions.Updated,
                        OldStatus = da.Status,
                        NewStatus = da.Status,
                        Remarks = $"Quotation items updated with max discount {maxDiscount:F2}%",
                        PerformedBy = actingUser,
                        PerformedOn = now
                    });
                }
                else
                {
                    var approvalNumber = await _numbering.GenerateNextApprovalNumberAsync(cancellationToken);
                    var approval = new DiscountApproval
                    {
                        ApprovalNumber = approvalNumber,
                        RequestDate = entity.QuotationDate,
                        SourceType = DiscountApprovalSourceTypes.Quotation,
                        QuotationId = entity.Id,
                        QuotationNumber = entity.QuotationNumber,
                        CustomerName = entity.CustomerName,
                        CustomerCategory = PriceListCustomerCategories.Standard,
                        SalesPersonUserId = 1,
                        RequestedDiscountPercentage = maxDiscount,
                        RequestedAmount = entity.DiscountTotal,
                        ApprovalLevel = DiscountApprovalLevels.Manager,
                        Priority = DiscountApprovalPriorities.Normal,
                        Status = DiscountApprovalStatuses.Pending,
                        Reason = $"Quotation line discount of {maxDiscount:F2}% exceeds 5.00% threshold.",
                        Remarks = $"Updated from Quotation {entity.QuotationNumber}",
                        CreatedBy = actingUser,
                        CreatedDate = now,
                        UpdatedBy = actingUser,
                        UpdatedDate = now,
                        IsDeleted = false
                    };

                    approval.History.Add(new DiscountApprovalHistory
                    {
                        Action = DiscountApprovalHistoryActions.Created,
                        OldStatus = string.Empty,
                        NewStatus = DiscountApprovalStatuses.Pending,
                        Remarks = $"Discount approval request created from updated Quotation {entity.QuotationNumber}",
                        PerformedBy = actingUser,
                        PerformedOn = now
                    });

                    _db.DiscountApprovals.Add(approval);
                    await _db.SaveChangesAsync(cancellationToken);
                    entity.DiscountApprovalId = approval.Id;
                }
            }
            else
            {
                if (entity.Status == QuotationStatuses.PendingDiscountApproval)
                {
                    entity.Status = QuotationStatuses.Draft;
                }
                entity.DiscountApprovalStatus = "None";
            }

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            if (entity.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Cannot delete a quotation that has been converted to a Sales Order.");
            }

            entity.IsDeleted = true;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<QuotationDto> MarkClientAcceptedAsync(
            int id,
            QuotationClientAcceptRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null)
            {
                throw new KeyNotFoundException($"Quotation with ID {id} not found.");
            }

            if (entity.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Quotation has already been converted to a Sales Order.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.Status = QuotationStatuses.Approved; // Approved strictly means Client Accepted
            entity.ClientPoNumber = request.ClientPoNumber?.Trim();
            entity.ClientAcceptedAt = request.ClientAcceptedAt ?? now;
            entity.ClientPoAttachmentUrl = request.ClientPoAttachmentUrl?.Trim();
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<QuotationDto> ReviseAsync(
            int id,
            QuotationReviseRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var revision = await _revisionService.CreateRevisionAsync(
                id,
                request?.Reason,
                actingUser,
                cancellationToken);

            return MapToDto(revision);
        }

        public async Task<QuotationDto> SubmitForApprovalAsync(
            int id,
            QuotationSubmitForApprovalRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var quote = await _db.Quotations
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Quotation with ID {id} not found.");

            if (quote.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Cannot submit a quotation that is already converted to a Sales Order.");
            }

            var eval = _complianceService.EvaluateQuotation(quote);
            var now = DateTimeOffset.UtcNow;

            if (eval.IsBlocked)
            {
                quote.Status = QuotationStatuses.PendingDiscountApproval;
                quote.DiscountApprovalStatus = DiscountApprovalStatuses.Pending;

                var approvalNumber = await _numbering.GenerateNextApprovalNumberAsync(cancellationToken);
                var approval = new DiscountApproval
                {
                    TenantId = quote.TenantId,
                    ApprovalNumber = approvalNumber,
                    RequestDate = quote.QuotationDate,
                    SourceType = DiscountApprovalSourceTypes.Quotation,
                    QuotationId = quote.Id,
                    QuotationNumber = quote.QuotationNumber,
                    CustomerName = quote.CustomerName,
                    CustomerCategory = quote.CustomerCategory,
                    SalesPersonUserId = quote.SalesPersonUserId ?? 1,
                    RequestedDiscountPercentage = eval.MaxDiscountBreachPercentage,
                    MaxAllowedDiscountPercentage = quote.Items.Count > 0 ? quote.Items.Max(i => i.MaxDiscountPctSnapshot) : 0m,
                    DiscountBreachPercentage = eval.MaxDiscountBreachPercentage,
                    RequestedAmount = quote.DiscountTotal,
                    ApprovalLevel = eval.RequiredApprovalLevel,
                    Priority = request.Priority ?? DiscountApprovalPriorities.Normal,
                    Status = DiscountApprovalStatuses.Pending,
                    Reason = string.Join("; ", eval.BreachReasons),
                    Remarks = request.Reason ?? $"Commercial deviation submission for {quote.QuotationNumber}",
                    CreatedBy = actingUser,
                    CreatedDate = now,
                    UpdatedBy = actingUser,
                    UpdatedDate = now,
                    IsDeleted = false
                };

                approval.History.Add(new DiscountApprovalHistory
                {
                    Action = DiscountApprovalHistoryActions.Submitted,
                    OldStatus = string.Empty,
                    NewStatus = DiscountApprovalStatuses.Pending,
                    Remarks = $"Submitted for commercial deviation approval. Level: {eval.RequiredApprovalLevel}",
                    PerformedBy = actingUser,
                    PerformedOn = now
                });

                _db.DiscountApprovals.Add(approval);
                await _db.SaveChangesAsync(cancellationToken);

                quote.DiscountApprovalId = approval.Id;
            }
            else if (quote.GrandTotal > 500_000m)
            {
                quote.Status = QuotationStatuses.PendingApproval;
                var appSeq = await _db.QuotationApprovalDocumentSequences.FirstOrDefaultAsync(cancellationToken);
                if (appSeq == null)
                {
                    appSeq = new QuotationApprovalDocumentSequence { LastNumber = 0, FinancialYear = DateTime.UtcNow.Year, Prefix = "QA" };
                    _db.QuotationApprovalDocumentSequences.Add(appSeq);
                }
                appSeq.LastNumber += 1;
                var appNum = $"QA-{DateTime.UtcNow.Year}-{appSeq.LastNumber:D4}";

                var approval = new QuotationApproval
                {
                    TenantId = quote.TenantId,
                    ApprovalNumber = appNum,
                    RequestDate = quote.QuotationDate,
                    QuotationId = quote.Id,
                    QuotationNumber = quote.QuotationNumber,
                    CustomerName = quote.CustomerName,
                    SalesPersonUserId = quote.SalesPersonUserId ?? 1,
                    TotalAmount = quote.GrandTotal,
                    ApprovalLevel = eval.RequiredApprovalLevel != "None" ? eval.RequiredApprovalLevel : "Manager",
                    Priority = request.Priority ?? "Normal",
                    Status = "Submitted",
                    Reason = request.Reason ?? $"High-value deal review (> 500,000 INR) for {quote.QuotationNumber}",
                    Remarks = $"Auto-routed for {eval.RequiredApprovalLevel} approval",
                    CreatedBy = actingUser,
                    CreatedDate = now,
                    UpdatedBy = actingUser,
                    UpdatedDate = now,
                    IsDeleted = false
                };

                approval.History.Add(new QuotationApprovalHistory
                {
                    Action = "Submitted",
                    OldStatus = "Draft",
                    NewStatus = "Submitted",
                    Remarks = "High-value quotation deal submitted for management review",
                    PerformedBy = actingUser,
                    PerformedOn = now
                });

                _db.QuotationApprovals.Add(approval);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                quote.Status = QuotationStatuses.Approved;
                quote.ApprovalToken = _complianceService.GenerateApprovalToken(quote);
            }

            quote.UpdatedBy = actingUser;
            quote.UpdatedDate = now;

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(quote);
        }

        public async Task<QuotationDto> DuplicateAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var existing = await _db.Quotations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (existing is null)
            {
                throw new KeyNotFoundException($"Quotation with ID {id} not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var number = await GetNextNumberAsync(cancellationToken);

            var clone = new Quotation
            {
                QuotationNumber = number,
                RevisionNumber = 1,
                IsCurrentRevision = true,
                CustomerId = existing.CustomerId,
                CustomerName = existing.CustomerName,
                ContactPerson = existing.ContactPerson,
                CustomerEmail = existing.CustomerEmail,
                CustomerPhone = existing.CustomerPhone,
                BillingAddress = existing.BillingAddress,
                ShippingAddress = existing.ShippingAddress,
                SalesPerson = actingUser,
                Status = QuotationStatuses.Draft,
                QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                Currency = existing.Currency,
                ExchangeRate = existing.ExchangeRate,
                Subtotal = existing.Subtotal,
                DiscountTotal = existing.DiscountTotal,
                TaxTotal = existing.TaxTotal,
                FreightAmount = existing.FreightAmount,
                PackagingAmount = existing.PackagingAmount,
                RoundOff = existing.RoundOff,
                GrandTotal = existing.GrandTotal,
                PaymentTerms = existing.PaymentTerms,
                DeliveryTerms = existing.DeliveryTerms,
                Notes = existing.Notes,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                IsDeleted = false
            };

            foreach (var item in existing.Items.OrderBy(i => i.SortOrder))
            {
                clone.Items.Add(new QuotationItem
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

            _db.Quotations.Add(clone);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(clone);
        }

        public async Task<QuotationDto> ChangeStatusAsync(
            int id,
            QuotationStatusChangeRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null)
            {
                throw new KeyNotFoundException($"Quotation with ID {id} not found.");
            }

            if (entity.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Converted quotation cannot change status.");
            }

            var newStatus = request.Status?.Trim();
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                throw new InvalidOperationException("Status cannot be empty.");
            }

            if (string.Equals(newStatus, "PendingApproval", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = QuotationStatuses.PendingApproval;
            }
            else if (string.Equals(newStatus, "RevisionRequired", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = QuotationStatuses.RevisionRequired;
            }

            var matchedStatus = QuotationStatuses.All.FirstOrDefault(s => string.Equals(s, newStatus, StringComparison.OrdinalIgnoreCase));
            if (matchedStatus == null)
            {
                throw new InvalidOperationException($"Invalid quotation status '{newStatus}'.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.Status = matchedStatus;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            if (!string.IsNullOrWhiteSpace(request.Remarks))
            {
                entity.Notes = string.IsNullOrWhiteSpace(entity.Notes)
                    ? $"[Status: {matchedStatus}] {request.Remarks}"
                    : $"{entity.Notes}\n[Status: {matchedStatus}] {request.Remarks}".Trim();
            }

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default)
        {
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
            var prefix = $"QT-{yearMonth}-";
            var count = await _db.Quotations
                .CountAsync(x => x.QuotationNumber.StartsWith(prefix), cancellationToken);

            return $"{prefix}{(count + 1):D4}";
        }

        private static void HydrateItems(
            Quotation quote,
            IEnumerable<QuotationItemUpsertDto> dtos,
            Dictionary<int, FinishedGood>? goodsById = null,
            Dictionary<string, FinishedGood>? goodsByCode = null)
        {
            var idx = 1;
            foreach (var dto in dtos)
            {
                FinishedGood? fg = null;
                var rawId = dto.ProductId ?? dto.ItemId;
                if (rawId.HasValue && goodsById != null && goodsById.TryGetValue(rawId.Value, out var foundById))
                {
                    fg = foundById;
                }
                else if (!string.IsNullOrWhiteSpace(dto.ItemCode) && goodsByCode != null && goodsByCode.TryGetValue(dto.ItemCode.Trim(), out var foundByCode))
                {
                    fg = foundByCode;
                }

                var resolvedProductId = fg?.Id ?? (rawId.HasValue && rawId.Value > 0 ? rawId : null);
                var resolvedWarehouseId = (dto.WarehouseId.HasValue && dto.WarehouseId.Value > 0)
                    ? dto.WarehouseId.Value
                    : (fg != null ? fg.WarehouseId : 1);

                var discountAmt = Math.Round(dto.Quantity * dto.UnitPrice * (dto.DiscountPercent / 100m), 2);
                var taxable = (dto.Quantity * dto.UnitPrice) - discountAmt;
                var taxAmt = Math.Round(taxable * (dto.TaxPercent / 100m), 2);
                var lineTotal = taxable + taxAmt;

                var itemType = Enum.TryParse<PricingItemType>(dto.ItemType, true, out var parsedType)
                    ? parsedType
                    : (fg != null || resolvedProductId.HasValue ? PricingItemType.FinishedGood : PricingItemType.Service);

                quote.Items.Add(new QuotationItem
                {
                    TenantId = quote.TenantId,
                    ItemType = itemType,
                    ItemId = resolvedProductId,
                    ProductId = resolvedProductId,
                    WarehouseId = resolvedWarehouseId,
                    BaseStockUom = string.IsNullOrWhiteSpace(dto.BaseStockUom) ? (fg?.Unit ?? dto.Unit ?? "Nos") : dto.BaseStockUom.Trim(),
                    LineNumber = idx,
                    SortOrder = dto.SortOrder > 0 ? dto.SortOrder : idx,
                    ItemCode = string.IsNullOrWhiteSpace(dto.ItemCode) ? (fg?.ProductCode ?? string.Empty) : dto.ItemCode,
                    ItemName = string.IsNullOrWhiteSpace(dto.ItemName) ? (fg?.ProductName ?? "Item") : dto.ItemName,
                    Description = dto.Description ?? string.Empty,
                    Quantity = dto.Quantity,
                    Unit = dto.Unit ?? (fg?.Unit ?? "NOS"),
                    UnitPrice = dto.UnitPrice,
                    DiscountPercent = dto.DiscountPercent,
                    DiscountAmount = discountAmt,
                    TaxPercent = dto.TaxPercent,
                    TaxAmount = taxAmt,
                    LineTotal = lineTotal
                });
                idx++;
            }
        }

        private static void CalculateTotals(Quotation quote)
        {
            quote.CalculateTotals();
        }

        private static QuotationDto MapToDto(Quotation q)
        {
            return new QuotationDto
            {
                Id = q.Id,
                TenantId = q.TenantId,
                QuotationNumber = q.QuotationNumber,
                ParentQuotationId = q.ParentQuotationId,
                RootQuotationId = q.RootQuotationId,
                RevisionNumber = q.RevisionNumber,
                IsCurrentRevision = q.IsCurrentRevision,
                CustomerId = q.CustomerId,
                CustomerName = q.CustomerName,
                CustomerCategory = q.CustomerCategory,
                ContactPerson = q.ContactPerson,
                CustomerEmail = q.CustomerEmail,
                CustomerPhone = q.CustomerPhone,
                BillingAddress = q.BillingAddress,
                ShippingAddress = q.ShippingAddress,
                SalesPerson = q.SalesPerson,
                SalesPersonUserId = q.SalesPersonUserId,
                Status = q.Status,
                QuotationDate = q.QuotationDate.ToString("yyyy-MM-dd"),
                ValidUntil = q.ValidUntil.ToString("yyyy-MM-dd"),
                Currency = q.Currency,
                ExchangeRate = q.ExchangeRate,
                Subtotal = q.Subtotal,
                DiscountTotal = q.DiscountTotal,
                TaxTotal = q.TaxTotal,
                FreightAmount = q.FreightAmount,
                PackagingAmount = q.PackagingAmount,
                RoundOff = q.RoundOff,
                GrandTotal = q.GrandTotal,
                PaymentTerms = q.PaymentTerms,
                DeliveryTerms = q.DeliveryTerms,
                Notes = q.Notes,
                ClientPoNumber = q.ClientPoNumber,
                ClientAcceptedAt = q.ClientAcceptedAt?.ToString("o"),
                ClientPoAttachmentUrl = q.ClientPoAttachmentUrl,
                ConvertedSalesOrderId = q.ConvertedSalesOrderId,
                ConvertedSalesOrderNumber = q.ConvertedSalesOrderNumber,
                ConvertedOn = q.ConvertedOn?.ToString("o"),
                DiscountApprovalId = q.DiscountApprovalId,
                DiscountApprovalStatus = q.DiscountApprovalStatus,
                ApprovalToken = q.ApprovalToken,
                CreatedBy = q.CreatedBy,
                CreatedDate = q.CreatedDate,
                UpdatedBy = q.UpdatedBy,
                UpdatedDate = q.UpdatedDate,
                Items = q.Items.OrderBy(i => i.SortOrder).Select(i => new QuotationItemDto
                {
                    Id = i.Id,
                    ItemType = i.ItemType.ToString(),
                    ItemId = i.ItemId,
                    ProductId = i.ProductId,
                    WarehouseId = i.WarehouseId,
                    BaseStockUom = i.BaseStockUom,
                    LineNumber = i.LineNumber,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    DiscountPercent = i.DiscountPercent,
                    DiscountAmount = i.DiscountAmount,
                    TaxPercent = i.TaxPercent,
                    TaxAmount = i.TaxAmount,
                    LineTotal = i.LineTotal,
                    SortOrder = i.SortOrder,
                    ResolvedPriceListId = i.ResolvedPriceListId,
                    PriceListNumberSnapshot = i.PriceListNumberSnapshot,
                    ResolutionStageSnapshot = i.ResolutionStageSnapshot,
                    OriginalSellingRate = i.OriginalSellingRate,
                    FloorPriceSnapshot = i.FloorPriceSnapshot,
                    MaxDiscountPctSnapshot = i.MaxDiscountPctSnapshot,
                    TaxSlabPercentSnapshot = i.TaxSlabPercentSnapshot,
                    SnapshotTimestampUtc = i.SnapshotTimestampUtc,
                    IsCommercialDeviation = i.IsCommercialDeviation
                }).ToList()
            };
        }
    }
}
