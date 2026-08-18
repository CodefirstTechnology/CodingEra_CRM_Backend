using System;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class StoreInventoryRules
    {
        public static bool CanTransitionTransfer(TransferStatus current, TransferStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                TransferStatus.Draft => target is TransferStatus.Approved or TransferStatus.Transferred or TransferStatus.Completed or TransferStatus.Cancelled,
                TransferStatus.Approved => target is TransferStatus.Transferred or TransferStatus.Completed or TransferStatus.Cancelled,
                TransferStatus.Transferred => target is TransferStatus.Completed or TransferStatus.Cancelled,
                TransferStatus.Completed => false,
                TransferStatus.Cancelled => false,
                _ => false
            };
        }

        public static bool CanTransitionVerification(VerificationStatus current, VerificationStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                VerificationStatus.Scheduled => target is VerificationStatus.InProgress or VerificationStatus.Completed or VerificationStatus.Adjusted or VerificationStatus.Cancelled,
                VerificationStatus.InProgress => target is VerificationStatus.Completed or VerificationStatus.Adjusted or VerificationStatus.Cancelled,
                VerificationStatus.Completed => target == VerificationStatus.Adjusted,
                VerificationStatus.Adjusted => false,
                VerificationStatus.Cancelled => false,
                _ => false
            };
        }

        public static string? ValidateStockOut(decimal availableStock, decimal requestedQuantity)
        {
            if (requestedQuantity <= 0)
            {
                return "Stock out quantity must be greater than zero.";
            }

            if (requestedQuantity > availableStock + 0.0001m)
            {
                return $"Requested stock out quantity ({requestedQuantity}) exceeds available stock ({availableStock}).";
            }

            return null;
        }

        public static StockAgeBand CalculateStockAgeBand(int ageDays)
        {
            if (ageDays <= 30) return StockAgeBand.Band0To30;
            if (ageDays <= 60) return StockAgeBand.Band31To60;
            if (ageDays <= 90) return StockAgeBand.Band61To90;
            return StockAgeBand.Band90Plus;
        }
    }
}
