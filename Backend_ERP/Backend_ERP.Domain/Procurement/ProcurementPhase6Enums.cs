namespace ERP.Domain.Procurement
{
    public enum IncomingInspectionStatus
    {
        Draft,
        Submitted,
        Approved,
        Rejected,
        Closed
    }

    public enum InspectionResult
    {
        Pending,
        Accepted,
        Rejected,
        Conditional
    }

    public enum InspectionType
    {
        Visual,
        Dimensional,
        Chemical,
        Mechanical,
        Full
    }

    public enum InspectionMethod
    {
        Check100Percent,
        Sampling,
        AQL,
        SkipLot
    }

    public enum InProcessQcStatus
    {
        Draft,
        Running,
        Passed,
        Failed,
        Closed
    }

    public enum QcCheckResult
    {
        Pending,
        Pass,
        Fail,
        Borderline
    }

    public enum FinalInspectionStatus
    {
        Draft,
        Submitted,
        Approved,
        Rejected,
        Closed
    }

    public enum LoadTestStatus
    {
        Pending,
        Passed,
        Failed
    }

    public enum CertificateStatus
    {
        Draft,
        Approved,
        Issued,
        Expired,
        Cancelled
    }

    public enum RejectionSource
    {
        Incoming,
        InProcess,
        FinalInspection
    }

    public enum RejectionAnalysisStatus
    {
        Open,
        UnderAnalysis,
        Closed
    }

    public enum Shift
    {
        A,
        B,
        C
    }
}
