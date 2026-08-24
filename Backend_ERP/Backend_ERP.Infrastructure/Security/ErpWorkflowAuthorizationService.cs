using System;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Domain.Enums;
using ERP.Domain.Sales;
using ERP.Shared.Security;

namespace ERP.Infrastructure.Security
{
    /// <summary>
    /// Implements 3-dimensional workflow authorization rules:
    /// Authenticated User + Specific ErpPermission + Record Ownership Scope + Valid Lifecycle State.
    /// </summary>
    public class ErpWorkflowAuthorizationService : IErpWorkflowAuthorizationService
    {
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;

        public ErpWorkflowAuthorizationService(
            ICurrentUser currentUser,
            IErpAuthorizationService authService)
        {
            _currentUser = currentUser;
            _authService = authService;
        }

        // =========================================================================
        // QUOTATION WORKFLOW AUTHORIZATION
        // =========================================================================

        public bool CanEditQuotation(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.Quotations.Edit)) return false;
            if (!_authService.CanAccessRecord(ownerUserId)) return false;

            var norm = QuotationApprovalStatusRules.Normalize(currentStatus);
            return norm != null && QuotationApprovalStatusRules.CanBeModified(norm);
        }

        public bool CanSubmitQuotation(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.Quotations.Submit)) return false;
            if (!_authService.CanAccessRecord(ownerUserId)) return false;

            var norm = QuotationApprovalStatusRules.Normalize(currentStatus);
            return norm != null && QuotationApprovalStatusRules.CanSubmit(norm);
        }

        public bool CanApproveQuotation(string? currentStatus, int? ownerUserId)
        {
            // Privileged operation - requires Approve permission (Admin only)
            if (!_authService.Authorize(ErpPermissions.Quotations.Approve)) return false;

            // Self-approval protection: Sales Executive cannot approve own records
            if (!_currentUser.IsAdmin && _currentUser.UserId.HasValue && ownerUserId.HasValue && _currentUser.UserId.Value == ownerUserId.Value)
            {
                return false;
            }

            var norm = QuotationApprovalStatusRules.Normalize(currentStatus);
            return norm is QuotationApprovalStatuses.Submitted or QuotationApprovalStatuses.UnderReview;
        }

        public bool CanRejectQuotation(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.Quotations.Reject)) return false;
            var norm = QuotationApprovalStatusRules.Normalize(currentStatus);
            return norm is QuotationApprovalStatuses.Submitted or QuotationApprovalStatuses.UnderReview;
        }

        public bool CanReturnQuotation(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.Quotations.Return)) return false;
            var norm = QuotationApprovalStatusRules.Normalize(currentStatus);
            return norm is QuotationApprovalStatuses.Submitted or QuotationApprovalStatuses.UnderReview;
        }

        public bool CanReopenQuotation(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.Quotations.Reopen)) return false;
            var norm = QuotationApprovalStatusRules.Normalize(currentStatus);
            return norm != null && QuotationApprovalStatusRules.CanReopen(norm);
        }

        public bool CanDeleteQuotation(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.Quotations.Delete)) return false;
            var norm = QuotationApprovalStatusRules.Normalize(currentStatus);
            return norm == QuotationApprovalStatuses.Draft;
        }

        public bool CanConvertQuotation(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.Quotations.Convert)) return false;
            if (!_authService.CanAccessRecord(ownerUserId)) return false;

            var norm = QuotationApprovalStatusRules.Normalize(currentStatus);
            return norm == QuotationApprovalStatuses.Approved;
        }

        // =========================================================================
        // DISCOUNT APPROVAL WORKFLOW AUTHORIZATION
        // =========================================================================

        public bool CanCreateDiscountApproval(int? quotationOwnerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.DiscountApprovals.Create)) return false;
            // Must own or have access to source quotation
            return _authService.CanAccessRecord(quotationOwnerUserId);
        }

        public bool CanEditDiscountApproval(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.DiscountApprovals.Create)) return false;
            if (!_authService.CanAccessRecord(ownerUserId)) return false;

            return DiscountApprovalStatusRules.IsEditable(currentStatus ?? string.Empty);
        }

        public bool CanResubmitDiscountApproval(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.DiscountApprovals.Resubmit)) return false;
            if (!_authService.CanAccessRecord(ownerUserId)) return false;

            var norm = DiscountApprovalStatusRules.Normalize(currentStatus);
            return norm == DiscountApprovalStatuses.Returned;
        }

        public bool CanApproveDiscountApproval(string? currentStatus, int? ownerUserId)
        {
            // Privileged operation - requires Approve permission (Admin only)
            if (!_authService.Authorize(ErpPermissions.DiscountApprovals.Approve)) return false;

            // Self-approval protection
            if (!_currentUser.IsAdmin && _currentUser.UserId.HasValue && ownerUserId.HasValue && _currentUser.UserId.Value == ownerUserId.Value)
            {
                return false;
            }

            var norm = DiscountApprovalStatusRules.Normalize(currentStatus);
            return norm is DiscountApprovalStatuses.Pending or DiscountApprovalStatuses.UnderReview;
        }

        public bool CanRejectDiscountApproval(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.DiscountApprovals.Reject)) return false;
            var norm = DiscountApprovalStatusRules.Normalize(currentStatus);
            return norm is DiscountApprovalStatuses.Pending or DiscountApprovalStatuses.UnderReview;
        }

        public bool CanReturnDiscountApproval(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.DiscountApprovals.Return)) return false;
            var norm = DiscountApprovalStatusRules.Normalize(currentStatus);
            return norm is DiscountApprovalStatuses.Pending or DiscountApprovalStatuses.UnderReview;
        }

        public bool CanCancelDiscountApproval(string? currentStatus, int? ownerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.DiscountApprovals.Cancel)) return false;
            var norm = DiscountApprovalStatusRules.Normalize(currentStatus);
            return norm is DiscountApprovalStatuses.Pending or DiscountApprovalStatuses.UnderReview or DiscountApprovalStatuses.Returned;
        }

        // =========================================================================
        // SALES ORDER WORKFLOW AUTHORIZATION
        // =========================================================================

        public bool CanEditSalesOrder(string? currentStatus, string? createdBy)
        {
            if (!_authService.Authorize(ErpPermissions.SalesOrders.Edit)) return false;
            if (!_authService.CanAccessRecord(createdBy)) return false;

            var norm = SalesOrderStatusRules.Normalize(currentStatus ?? string.Empty);
            // Once Confirmed, Processing, Delivered, Completed or Cancelled, editing is locked
            return norm is SalesOrderStatuses.Draft or SalesOrderStatuses.Submitted;
        }

        public bool CanSubmitSalesOrder(string? currentStatus, string? createdBy)
        {
            if (!_authService.Authorize(ErpPermissions.SalesOrders.Submit)) return false;
            if (!_authService.CanAccessRecord(createdBy)) return false;

            var norm = SalesOrderStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm == SalesOrderStatuses.Draft;
        }

        public bool CanConfirmSalesOrder(string? currentStatus, string? createdBy)
        {
            if (!_authService.Authorize(ErpPermissions.SalesOrders.Confirm)) return false;
            var norm = SalesOrderStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is SalesOrderStatuses.Submitted or SalesOrderStatuses.Draft;
        }

        public bool CanProcessSalesOrder(string? currentStatus, string? createdBy)
        {
            if (!_authService.Authorize(ErpPermissions.SalesOrders.Process)) return false;
            var norm = SalesOrderStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm == SalesOrderStatuses.Confirmed;
        }

        public bool CanCompleteSalesOrder(string? currentStatus, string? createdBy)
        {
            if (!_authService.Authorize(ErpPermissions.SalesOrders.Complete)) return false;
            var norm = SalesOrderStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is SalesOrderStatuses.Processing or SalesOrderStatuses.PartiallyDelivered;
        }

        public bool CanCancelSalesOrder(string? currentStatus, string? createdBy)
        {
            if (!_authService.Authorize(ErpPermissions.SalesOrders.Cancel)) return false;
            var norm = SalesOrderStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm != null && norm != SalesOrderStatuses.Completed && norm != SalesOrderStatuses.Cancelled;
        }

        public bool CanCreateSalesOrderFromQuotation(string? quotationStatus, int? quotationOwnerUserId)
        {
            if (!_authService.Authorize(ErpPermissions.SalesOrders.Create)) return false;
            if (!_authService.CanAccessRecord(quotationOwnerUserId)) return false;

            var norm = QuotationApprovalStatusRules.Normalize(quotationStatus);
            return norm == QuotationApprovalStatuses.Approved;
        }

        // =========================================================================
        // PROFORMA INVOICE WORKFLOW AUTHORIZATION
        // =========================================================================

        public bool CanEditProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy)
        {
            if (!_authService.Authorize(ErpPermissions.ProformaInvoices.Edit)) return false;
            if (!_authService.CanAccessRecord(salesPersonOrCreatedBy)) return false;

            var norm = ProformaInvoiceStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is ProformaInvoiceStatuses.Draft or ProformaInvoiceStatuses.Returned or ProformaInvoiceStatuses.Rejected;
        }

        public bool CanSubmitProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy)
        {
            if (!_authService.Authorize(ErpPermissions.ProformaInvoices.Submit)) return false;
            if (!_authService.CanAccessRecord(salesPersonOrCreatedBy)) return false;

            var norm = ProformaInvoiceStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is ProformaInvoiceStatuses.Draft or ProformaInvoiceStatuses.Returned;
        }

        public bool CanApproveProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy)
        {
            if (!_authService.Authorize(ErpPermissions.ProformaInvoices.Approve)) return false;

            // Self-action protection
            if (!_currentUser.IsAdmin && _currentUser.UserId.HasValue && int.TryParse(salesPersonOrCreatedBy, out var ownerId) && _currentUser.UserId.Value == ownerId)
            {
                return false;
            }

            var norm = ProformaInvoiceStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is ProformaInvoiceStatuses.Submitted or ProformaInvoiceStatuses.PendingFinanceApproval;
        }

        public bool CanRejectProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy)
        {
            if (!_authService.Authorize(ErpPermissions.ProformaInvoices.Reject)) return false;
            var norm = ProformaInvoiceStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is ProformaInvoiceStatuses.Submitted or ProformaInvoiceStatuses.PendingFinanceApproval;
        }

        public bool CanReturnProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy)
        {
            if (!_authService.Authorize(ErpPermissions.ProformaInvoices.Return)) return false;
            var norm = ProformaInvoiceStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is ProformaInvoiceStatuses.Submitted or ProformaInvoiceStatuses.PendingFinanceApproval;
        }

        public bool CanConvertProformaInvoice(string? currentStatus, bool isAlreadyConverted)
        {
            if (!_authService.Authorize(ErpPermissions.ProformaInvoices.Convert)) return false;
            if (isAlreadyConverted) return false;

            var norm = ProformaInvoiceStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is ProformaInvoiceStatuses.Approved or ProformaInvoiceStatuses.Sent or ProformaInvoiceStatuses.Accepted;
        }

        public bool CanCreateProformaFromSalesOrder(string? salesOrderStatus, string? salesOrderCreatedBy)
        {
            if (!_authService.Authorize(ErpPermissions.ProformaInvoices.Create)) return false;
            return _authService.CanAccessRecord(salesOrderCreatedBy);
        }

        // =========================================================================
        // ADVANCE PAYMENT WORKFLOW AUTHORIZATION
        // =========================================================================

        public bool CanEditAdvancePayment(string? currentStatus, string? createdBy)
        {
            if (!_authService.Authorize(ErpPermissions.AdvancePayments.Edit)) return false;
            if (!_authService.CanAccessRecord(createdBy)) return false;

            var norm = AdvancePaymentStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm == AdvancePaymentStatuses.Draft;
        }

        public bool CanSubmitAdvancePayment(string? currentStatus, string? createdBy)
        {
            if (!_authService.Authorize(ErpPermissions.AdvancePayments.Create)) return false;
            if (!_authService.CanAccessRecord(createdBy)) return false;

            var norm = AdvancePaymentStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm == AdvancePaymentStatuses.Draft;
        }

        public bool CanVerifyAdvancePayment(string? currentStatus)
        {
            if (!_authService.Authorize(ErpPermissions.AdvancePayments.Verify)) return false;
            var norm = AdvancePaymentStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm == AdvancePaymentStatuses.Submitted;
        }

        public bool CanReceiveAdvancePayment(string? currentStatus)
        {
            if (!_authService.Authorize(ErpPermissions.AdvancePayments.Receive)) return false;
            var norm = AdvancePaymentStatusRules.Normalize(currentStatus ?? string.Empty);
            return norm is AdvancePaymentStatuses.Submitted or AdvancePaymentStatuses.FinanceVerification;
        }

        public bool CanApplyAdvancePayment(string? currentStatus, decimal remainingAmount, string? targetSalesOrderCreatedBy)
        {
            if (!_authService.Authorize(ErpPermissions.AdvancePayments.Apply)) return false;
            if (!_authService.CanAccessRecord(targetSalesOrderCreatedBy)) return false;

            if (remainingAmount <= 0) return false;
            return AdvancePaymentStatusRules.CanApply(currentStatus ?? string.Empty);
        }
    }
}
