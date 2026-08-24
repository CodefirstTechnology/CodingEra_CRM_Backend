using System;
using ERP.Domain.Sales;

namespace ERP.Application.Common.Security
{
    /// <summary>
    /// Centralized contract for 3-dimensional workflow authorization:
    /// Validates Authentication AND Permission AND Ownership Scope AND Workflow State.
    /// </summary>
    public interface IErpWorkflowAuthorizationService
    {
        // ---------------------------------------------------------------------
        // Quotation Workflow Authorization
        // ---------------------------------------------------------------------
        bool CanEditQuotation(string? currentStatus, int? ownerUserId);
        bool CanSubmitQuotation(string? currentStatus, int? ownerUserId);
        bool CanApproveQuotation(string? currentStatus, int? ownerUserId);
        bool CanRejectQuotation(string? currentStatus, int? ownerUserId);
        bool CanReturnQuotation(string? currentStatus, int? ownerUserId);
        bool CanReopenQuotation(string? currentStatus, int? ownerUserId);
        bool CanDeleteQuotation(string? currentStatus, int? ownerUserId);
        bool CanConvertQuotation(string? currentStatus, int? ownerUserId);

        // ---------------------------------------------------------------------
        // Discount Approval Workflow Authorization
        // ---------------------------------------------------------------------
        bool CanCreateDiscountApproval(int? quotationOwnerUserId);
        bool CanEditDiscountApproval(string? currentStatus, int? ownerUserId);
        bool CanResubmitDiscountApproval(string? currentStatus, int? ownerUserId);
        bool CanApproveDiscountApproval(string? currentStatus, int? ownerUserId);
        bool CanRejectDiscountApproval(string? currentStatus, int? ownerUserId);
        bool CanReturnDiscountApproval(string? currentStatus, int? ownerUserId);
        bool CanCancelDiscountApproval(string? currentStatus, int? ownerUserId);

        // ---------------------------------------------------------------------
        // Sales Order Workflow Authorization
        // ---------------------------------------------------------------------
        bool CanEditSalesOrder(string? currentStatus, string? createdBy);
        bool CanSubmitSalesOrder(string? currentStatus, string? createdBy);
        bool CanConfirmSalesOrder(string? currentStatus, string? createdBy);
        bool CanProcessSalesOrder(string? currentStatus, string? createdBy);
        bool CanCompleteSalesOrder(string? currentStatus, string? createdBy);
        bool CanCancelSalesOrder(string? currentStatus, string? createdBy);
        bool CanCreateSalesOrderFromQuotation(string? quotationStatus, int? quotationOwnerUserId);

        // ---------------------------------------------------------------------
        // Proforma Invoice Workflow Authorization
        // ---------------------------------------------------------------------
        bool CanEditProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy);
        bool CanSubmitProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy);
        bool CanApproveProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy);
        bool CanRejectProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy);
        bool CanReturnProformaInvoice(string? currentStatus, string? salesPersonOrCreatedBy);
        bool CanConvertProformaInvoice(string? currentStatus, bool isAlreadyConverted);
        bool CanCreateProformaFromSalesOrder(string? salesOrderStatus, string? salesOrderCreatedBy);

        // ---------------------------------------------------------------------
        // Advance Payment Workflow Authorization
        // ---------------------------------------------------------------------
        bool CanEditAdvancePayment(string? currentStatus, string? createdBy);
        bool CanSubmitAdvancePayment(string? currentStatus, string? createdBy);
        bool CanVerifyAdvancePayment(string? currentStatus);
        bool CanReceiveAdvancePayment(string? currentStatus);
        bool CanApplyAdvancePayment(string? currentStatus, decimal remainingAmount, string? targetSalesOrderCreatedBy);
    }
}
