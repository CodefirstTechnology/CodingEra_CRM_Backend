using System;
using System.Collections.Generic;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class QualityControlRules
    {
        public static bool CanTransitionIncoming(IncomingInspectionStatus current, IncomingInspectionStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                IncomingInspectionStatus.Draft => target is IncomingInspectionStatus.Submitted or IncomingInspectionStatus.Closed,
                IncomingInspectionStatus.Submitted => target is IncomingInspectionStatus.Approved or IncomingInspectionStatus.Rejected or IncomingInspectionStatus.Closed,
                IncomingInspectionStatus.Approved => target == IncomingInspectionStatus.Closed,
                IncomingInspectionStatus.Rejected => target == IncomingInspectionStatus.Closed,
                IncomingInspectionStatus.Closed => false,
                _ => false
            };
        }

        public static bool CanTransitionInProcess(InProcessQcStatus current, InProcessQcStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                InProcessQcStatus.Draft => target is InProcessQcStatus.Running or InProcessQcStatus.Closed,
                InProcessQcStatus.Running => target is InProcessQcStatus.Passed or InProcessQcStatus.Failed or InProcessQcStatus.Closed,
                InProcessQcStatus.Passed => target == InProcessQcStatus.Closed,
                InProcessQcStatus.Failed => target == InProcessQcStatus.Closed,
                InProcessQcStatus.Closed => false,
                _ => false
            };
        }

        public static bool CanTransitionFinal(FinalInspectionStatus current, FinalInspectionStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                FinalInspectionStatus.Draft => target is FinalInspectionStatus.Submitted or FinalInspectionStatus.Closed,
                FinalInspectionStatus.Submitted => target is FinalInspectionStatus.Approved or FinalInspectionStatus.Rejected or FinalInspectionStatus.Closed,
                FinalInspectionStatus.Approved => target == FinalInspectionStatus.Closed,
                FinalInspectionStatus.Rejected => target == FinalInspectionStatus.Closed,
                FinalInspectionStatus.Closed => false,
                _ => false
            };
        }

        public static bool CanTransitionCertificate(CertificateStatus current, CertificateStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                CertificateStatus.Draft => target is CertificateStatus.Approved or CertificateStatus.Issued or CertificateStatus.Cancelled,
                CertificateStatus.Approved => target is CertificateStatus.Issued or CertificateStatus.Cancelled,
                CertificateStatus.Issued => target is CertificateStatus.Expired or CertificateStatus.Cancelled,
                CertificateStatus.Expired => false,
                CertificateStatus.Cancelled => false,
                _ => false
            };
        }

        public static bool CanTransitionRejection(RejectionAnalysisStatus current, RejectionAnalysisStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                RejectionAnalysisStatus.Open => target is RejectionAnalysisStatus.UnderAnalysis or RejectionAnalysisStatus.Closed,
                RejectionAnalysisStatus.UnderAnalysis => target == RejectionAnalysisStatus.Closed,
                RejectionAnalysisStatus.Closed => false,
                _ => false
            };
        }

        public static string? ValidateIncomingQuantities(decimal sampling, decimal accepted, decimal rejected, decimal pending)
        {
            if (sampling < 0 || accepted < 0 || rejected < 0 || pending < 0)
            {
                return "Inspection quantities cannot be negative.";
            }

            if (accepted + rejected > sampling + 0.0001m && sampling > 0)
            {
                return "Accepted + rejected quantity cannot exceed sampling quantity.";
            }

            return null;
        }

        public static string? ValidateFinalInspectionQuantities(decimal accepted, decimal rejected, decimal sourceGoodQty)
        {
            if (accepted < 0 || rejected < 0)
            {
                return "Quantities cannot be negative.";
            }
            if (accepted + rejected > sourceGoodQty)
            {
                return "Accepted + rejected quantity cannot exceed production entry good quantity.";
            }
            return null;
        }

        public static string? ValidateRejectionQuantities(decimal quantity, decimal sourceLimit)
        {
            if (quantity <= 0)
            {
                return "Quantity must be greater than zero.";
            }
            if (quantity > sourceLimit)
            {
                return "Rejection quantity cannot exceed applicable production quantity.";
            }
            return null;
        }
    }
}
