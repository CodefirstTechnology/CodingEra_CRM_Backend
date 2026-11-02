using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IAccountsPayableAgingService
    {
        Task<AccountsPayableAgingGridDto> CalculateAgingScheduleAsync(DateTime asOfDate, int? vendorId = null, CancellationToken ct = default);
        Task<List<MsmeComplianceAlertDto>> GetMsmeSection43BAlertsAsync(CancellationToken ct = default);
    }
}

