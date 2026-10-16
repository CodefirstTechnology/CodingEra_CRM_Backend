using System.Threading.Tasks;
using CRM.DTO;

namespace CRM.Services
{
    public interface ILeadTrackerService
    {
        Task<LeadTrackerPagedResultDto> GetPagedTrackerDataAsync(LeadTrackerQueryParams filters);
        Task<byte[]> GenerateExcelExportAsync(LeadTrackerQueryParams filters);
    }
}
