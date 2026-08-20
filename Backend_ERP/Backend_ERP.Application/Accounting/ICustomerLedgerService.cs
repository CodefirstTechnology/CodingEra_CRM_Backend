using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;

namespace ERP.Application.Accounting
{
    public interface ICustomerLedgerService
    {
        Task<IReadOnlyList<CustomerLedgerListItemDto>> GetCustomerLedgerAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<CustomerLedgerEntryDto?> GetCustomerLedgerByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<CustomerStatementResultDto?> GetCustomerStatementAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerLedgerDashboardDto> GetCustomerLedgerDashboardAsync(CancellationToken cancellationToken = default);
        Task<CustomerLedgerEntryDto> CreateLedgerEntryAsync(CustomerLedgerEntryDto dto, string createdBy, CancellationToken cancellationToken = default);
    }

    public class CustomerStatementResultDto
    {
        public CustomerStatementSummaryDto Summary { get; set; } = new();
        public List<CustomerLedgerEntryDto> Entries { get; set; } = new();
    }
}
