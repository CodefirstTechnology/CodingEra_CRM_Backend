using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.DTO;

namespace CRM.Services
{
    public interface IDailySalesRecognitionService
    {
        Task<DailySalesRecognitionLogDto> AutofillExecutiveMetricsAsync(DateTime date, string executiveName);
        Task<DailySalesRecognitionDashboardResponseDto> GetDashboardDataAsync(DateTime? startDate, DateTime? endDate, string? executive);
        Task<DailySalesRecognitionLogDto> UpsertLogAsync(DailySalesRecognitionUpsertDto dto);
        Task<List<DailySalesRecognitionLogDto>> BulkUpsertLogsAsync(List<DailySalesRecognitionUpsertDto> dtos);
        Task<bool> DeleteLogAsync(int id);
        Task<List<string>> GetActiveExecutivesAsync();
    }
}
