using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class ProcurementAuditTrailService : IProcurementAuditTrailService
    {
        private readonly ERPDbContext _dbContext;

        public ProcurementAuditTrailService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<AuditTrailEntryDto>> GetAuditEntriesAsync(AuditTrailFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.ProcurementAuditTrailEntries.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Module))
            {
                var mod = query.Module.Trim().ToLower();
                q = q.Where(x => x.Module.ToLower() == mod);
            }

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                var act = query.Action.Trim().ToLower();
                q = q.Where(x => x.Action.ToLower() == act);
            }

            if (query.EntityId.HasValue)
            {
                q = q.Where(x => x.EntityId == query.EntityId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityNumber))
            {
                var num = query.EntityNumber.Trim().ToLower();
                q = q.Where(x => x.EntityNumber.ToLower().Contains(num));
            }

            if (!string.IsNullOrWhiteSpace(query.User))
            {
                var usr = query.User.Trim().ToLower();
                q = q.Where(x => x.User.ToLower().Contains(usr));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(x => x.EntityNumber.ToLower().Contains(s) ||
                                 x.User.ToLower().Contains(s) ||
                                 x.Action.ToLower().Contains(s) ||
                                 x.Remarks.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(query.DateFrom) && DateTime.TryParse(query.DateFrom, out var dFrom))
            {
                q = q.Where(x => x.Date >= dFrom.ToUniversalTime());
            }

            if (!string.IsNullOrWhiteSpace(query.DateTo) && DateTime.TryParse(query.DateTo, out var dTo))
            {
                q = q.Where(x => x.Date <= dTo.ToUniversalTime().AddDays(1));
            }

            var list = await q.OrderByDescending(x => x.Id).Take(500).ToListAsync(cancellationToken);

            return list.Select(MapEntry).ToList();
        }

        public async Task<List<AuditTrailEntryDto>> GetByEntityAsync(string module, int entityId, CancellationToken cancellationToken = default)
        {
            var mod = module.Trim().ToLower();
            var list = await _dbContext.ProcurementAuditTrailEntries
                .AsNoTracking()
                .Where(x => x.Module.ToLower() == mod && x.EntityId == entityId)
                .OrderByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return list.Select(MapEntry).ToList();
        }

        public async Task<AuditTrailEntryDto> RecordAsync(AuditTrailRecordInputDto input, string currentUser, CancellationToken cancellationToken = default)
        {
            var user = !string.IsNullOrWhiteSpace(input.User) ? input.User : currentUser;
            var dt = input.Date ?? DateTime.UtcNow;

            var entity = new ProcurementAuditTrailEntry
            {
                Module = string.IsNullOrWhiteSpace(input.Module) ? "Purchase Order" : input.Module,
                Action = string.IsNullOrWhiteSpace(input.Action) ? "Updated" : input.Action,
                EntityId = input.EntityId,
                EntityNumber = input.EntityNumber ?? string.Empty,
                User = user,
                Date = dt,
                OldValue = input.OldValue ?? string.Empty,
                NewValue = input.NewValue ?? string.Empty,
                Remarks = input.Remarks ?? string.Empty
            };

            _dbContext.ProcurementAuditTrailEntries.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapEntry(entity);
        }

        private static AuditTrailEntryDto MapEntry(ProcurementAuditTrailEntry e) => new()
        {
            Id = e.Id.ToString(),
            Module = e.Module,
            Action = e.Action,
            EntityId = e.EntityId,
            EntityNumber = e.EntityNumber,
            User = e.User,
            Date = e.Date,
            OldValue = e.OldValue,
            NewValue = e.NewValue,
            Remarks = e.Remarks
        };
    }
}
