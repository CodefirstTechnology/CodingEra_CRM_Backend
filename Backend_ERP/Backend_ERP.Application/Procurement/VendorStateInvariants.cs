using System;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class VendorStateInvariants
    {
        public static void AssertCanReleasePurchaseOrder(Vendor vendor, decimal newPoAmount)
        {
            if (vendor.Status != VendorStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Procure-to-Pay Constraint Violation: Cannot release Purchase Order. Vendor '{vendor.VendorCode}' is in '{vendor.Status}' status. POs strictly require 'Active' status.");
            }

            if (!vendor.IsKycVerified)
            {
                throw new InvalidOperationException(
                    $"Procure-to-Pay Constraint Violation: Vendor '{vendor.VendorCode}' has not completed mandatory verified KYC documentation.");
            }

            if (vendor.RatingScore < 60.00m)
            {
                throw new InvalidOperationException(
                    $"Quality Control Circuit-Breaker: Vendor '{vendor.VendorCode}' rating score ({vendor.RatingScore:F2}/100) has dropped below threshold 60.00. Sourcing is blocked pending QA CAPA audit.");
            }

            if (vendor.CreditLimit > 0 && (vendor.TotalOpenCommitment + newPoAmount) > vendor.CreditLimit)
            {
                throw new InvalidOperationException(
                    $"Credit Risk Violation: Vendor '{vendor.VendorCode}' credit ceiling breached. Current Open Commitment: {vendor.TotalOpenCommitment:C}, New PO: {newPoAmount:C}, Max Allowed Credit Limit: {vendor.CreditLimit:C}.");
            }
        }

        public static void AssertCanIssueRfq(Vendor vendor)
        {
            if (vendor.Status != VendorStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Cannot include Vendor '{vendor.VendorCode}' in RFQ. Vendor status is '{vendor.Status}'. Only Active vendors are eligible.");
            }

            if (vendor.RatingScore < 60.00m)
            {
                throw new InvalidOperationException(
                    $"Cannot include Vendor '{vendor.VendorCode}' in RFQ. Rating score ({vendor.RatingScore:F2}) is below acceptable threshold.");
            }
        }
    }
}

