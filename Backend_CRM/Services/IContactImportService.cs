using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.DTO;

namespace CRM.Services
{
    public interface IContactImportService
    {
        Task<ContactImportResultDto> ValidateImportAsync(
            IReadOnlyList<ContactImportRowDto> rows,
            CancellationToken cancellationToken = default);

        Task<ContactImportCommitResultDto> CommitImportAsync(
            int userId,
            IReadOnlyList<ContactImportRowDto> rows,
            CancellationToken cancellationToken = default);
    }
}
