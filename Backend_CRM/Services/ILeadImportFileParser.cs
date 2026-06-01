using CRM.DTO;

namespace CRM.Services
{
    /// <summary>Parses uploaded .xlsx or .csv lead import files into row DTOs.</summary>
    public interface ILeadImportFileParser
    {
        Task<IReadOnlyList<LeadImportRowDto>> ParseAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken = default);
    }
}
