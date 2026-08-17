using System;
using System.Collections.Generic;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class RFQRules
    {
        public static bool CanTransition(RFQStatus current, RFQStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                RFQStatus.Draft => target is RFQStatus.Sent or RFQStatus.Cancelled,
                RFQStatus.Sent => target is RFQStatus.VendorResponsesReceived or RFQStatus.ComparisonReady or RFQStatus.Cancelled,
                RFQStatus.VendorResponsesReceived => target is RFQStatus.ComparisonReady or RFQStatus.Awarded or RFQStatus.Cancelled,
                RFQStatus.ComparisonReady => target is RFQStatus.Awarded or RFQStatus.Closed or RFQStatus.Cancelled,
                RFQStatus.Awarded => target is RFQStatus.Closed,
                RFQStatus.Closed => false,
                RFQStatus.Cancelled => false,
                _ => false
            };
        }

        public static void ValidateCreate(
            DateTime rfqDate,
            DateTime dueDate,
            IReadOnlyList<(string ItemName, decimal Quantity)> lines)
        {
            if (dueDate < rfqDate.Date)
            {
                throw new InvalidOperationException("RFQ Due Date cannot be earlier than RFQ Date.");
            }

            if (lines == null || lines.Count == 0)
            {
                throw new InvalidOperationException("At least one line item is required for Request for Quotation.");
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.ItemName))
                {
                    throw new InvalidOperationException("Item name cannot be empty in RFQ lines.");
                }

                if (line.Quantity <= 0)
                {
                    throw new InvalidOperationException($"Line item '{line.ItemName}' quantity must be greater than zero.");
                }
            }
        }
    }
}
