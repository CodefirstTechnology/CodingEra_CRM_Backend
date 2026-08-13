using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CRM.DTO;

namespace CRM.Services
{
    /// <summary>Parses uploaded .xlsx or .csv contact import files into row DTOs.</summary>
    public interface IContactImportFileParser
    {
        Task<IReadOnlyList<ContactImportRowDto>> ParseAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken = default);
    }
}
