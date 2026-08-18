using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IQualityControlService
    {
        // ── Incoming Inspection ──
        Task<PagedResult<IncomingListItemDto>> GetIncomingInspectionsAsync(ListQueryDto query, CancellationToken cancellationToken = default);
        Task<IncomingDto?> GetIncomingByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IncomingDto> CreateIncomingAsync(IncomingCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<IncomingDto?> UpdateIncomingAsync(int id, IncomingCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<bool> DeleteIncomingAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<IncomingDto?> DuplicateIncomingAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<IncomingDto?> SubmitIncomingAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<IncomingDto?> ApproveIncomingAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<IncomingDto?> RejectIncomingAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<IncomingDto?> CloseIncomingAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<IncomingDto?> RecordInspectionAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<IncomingDashboardDto> GetIncomingDashboardAsync(CancellationToken cancellationToken = default);

        // ── In-Process QC ──
        Task<PagedResult<InProcessListItemDto>> GetInProcessChecksAsync(ListQueryDto query, CancellationToken cancellationToken = default);
        Task<InProcessDto?> GetInProcessByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<InProcessDto> CreateInProcessAsync(InProcessCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<InProcessDto?> UpdateInProcessAsync(int id, InProcessCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<bool> DeleteInProcessAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<InProcessDto?> StartInProcessAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<InProcessDto?> RecordInProcessResultAsync(int id, string? actualValue, string? result, string actingUser, CancellationToken cancellationToken = default);
        Task<InProcessDto?> PassInProcessAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<InProcessDto?> FailInProcessAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<InProcessDto?> CloseInProcessAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<InProcessDashboardDto> GetInProcessDashboardAsync(CancellationToken cancellationToken = default);

        // ── Final Inspection ──
        Task<PagedResult<FinalListItemDto>> GetFinalInspectionsAsync(ListQueryDto query, CancellationToken cancellationToken = default);
        Task<FinalDto?> GetFinalByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<FinalDto> CreateFinalAsync(FinalCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<FinalDto?> UpdateFinalAsync(int id, FinalCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<bool> DeleteFinalAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<FinalDto?> SubmitFinalAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<FinalDto?> ApproveFinalAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<FinalDto?> RejectFinalAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<FinalDto?> CloseFinalAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<CertificateDto> GenerateCertificateAsync(int finalId, string actingUser, CancellationToken cancellationToken = default);
        Task<LoadTestDto> GenerateLoadReportAsync(int finalId, string actingUser, CancellationToken cancellationToken = default);
        Task<FinalDashboardDto> GetFinalDashboardAsync(CancellationToken cancellationToken = default);

        // ── Load Test ──
        Task<PagedResult<LoadTestListItemDto>> GetLoadTestsAsync(ListQueryDto query, CancellationToken cancellationToken = default);
        Task<LoadTestDto?> GetLoadTestByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<LoadTestDto> CreateLoadTestAsync(LoadTestCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<LoadTestDashboardDto> GetLoadTestDashboardAsync(CancellationToken cancellationToken = default);

        // ── Test Certificate ──
        Task<PagedResult<CertificateListItemDto>> GetCertificatesAsync(ListQueryDto query, CancellationToken cancellationToken = default);
        Task<CertificateDto?> GetCertificateByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<CertificateDto> CreateCertificateAsync(CertificateCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<CertificateDto?> UpdateCertificateAsync(int id, CertificateCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<bool> DeleteCertificateAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<CertificateDto?> DuplicateCertificateAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<CertificateDto?> ApproveCertificateAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<CertificateDto?> IssueCertificateAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<CertificateDto?> CancelCertificateAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<CertificateDashboardDto> GetCertificateDashboardAsync(CancellationToken cancellationToken = default);

        // ── Rejection Analysis ──
        Task<PagedResult<RejectionListItemDto>> GetRejectionsAsync(ListQueryDto query, CancellationToken cancellationToken = default);
        Task<RejectionDto?> GetRejectionByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<RejectionDto> RecordRejectionAsync(RejectionCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<RejectionDto?> StartRejectionAnalysisAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<RejectionDto?> CloseRejectionAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<RejectionDashboardDto> GetRejectionDashboardAsync(CancellationToken cancellationToken = default);
        Task<RejectionReportDto> GetRejectionReportAsync(CancellationToken cancellationToken = default);
    }
}
