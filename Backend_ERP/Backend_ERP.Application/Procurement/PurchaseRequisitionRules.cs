using System;
using System.Collections.Generic;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class PurchaseRequisitionRules
    {
        public static bool CanTransition(PurchaseRequisitionStatus current, PurchaseRequisitionStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                PurchaseRequisitionStatus.Draft => target is PurchaseRequisitionStatus.Submitted or PurchaseRequisitionStatus.Closed,
                PurchaseRequisitionStatus.Submitted => target is PurchaseRequisitionStatus.Approved or PurchaseRequisitionStatus.Rejected or PurchaseRequisitionStatus.Draft,
                PurchaseRequisitionStatus.Approved => target is PurchaseRequisitionStatus.ConvertedToRFQ or PurchaseRequisitionStatus.Closed,
                PurchaseRequisitionStatus.ConvertedToRFQ => target is PurchaseRequisitionStatus.Closed,
                PurchaseRequisitionStatus.Rejected => target is PurchaseRequisitionStatus.Draft,
                PurchaseRequisitionStatus.Closed => false,
                _ => false
            };
        }

        public static void ValidateCreate(
            string department,
            string requestor,
            DateTime requiredDate,
            IReadOnlyList<(string ItemName, decimal Quantity, decimal EstimatedPrice)> lines)
        {
            if (string.IsNullOrWhiteSpace(department))
            {
                throw new InvalidOperationException("Department is required for Purchase Requisition.");
            }

            if (string.IsNullOrWhiteSpace(requestor))
            {
                throw new InvalidOperationException("Requestor is required for Purchase Requisition.");
            }

            if (lines == null || lines.Count == 0)
            {
                throw new InvalidOperationException("At least one line item is required for Purchase Requisition.");
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.ItemName))
                {
                    throw new InvalidOperationException("Item name cannot be empty in Purchase Requisition lines.");
                }

                if (line.Quantity <= 0)
                {
                    throw new InvalidOperationException($"Line item '{line.ItemName}' quantity must be greater than zero.");
                }

                if (line.EstimatedPrice < 0)
                {
                    throw new InvalidOperationException($"Line item '{line.ItemName}' estimated price cannot be negative.");
                }
            }
        }
    }
}
