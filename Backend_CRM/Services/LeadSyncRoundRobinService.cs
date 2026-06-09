using CRM.DATA;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface ILeadSyncRoundRobinService
    {
        /// <summary>
        /// When lead notes contain a marketplace sync marker, assigns the next round-robin owner.
        /// Returns true when an owner was applied.
        /// </summary>
        Task<bool> TryApplyOwnerForSyncLeadAsync(Lead lead, CancellationToken cancellationToken = default);

        Task<int?> PeekNextOwnerIdAsync(int sourceId, CancellationToken cancellationToken = default);
    }

    public class LeadSyncRoundRobinService : ILeadSyncRoundRobinService
    {
        private readonly TaskDbcontext _db;

        public LeadSyncRoundRobinService(TaskDbcontext db)
        {
            _db = db;
        }

        public async Task<bool> TryApplyOwnerForSyncLeadAsync(
            Lead lead,
            CancellationToken cancellationToken = default)
        {
            var marker = LeadSyncNotesHelper.TryExtractMarkerName(lead.Notes);
            if (string.IsNullOrWhiteSpace(marker))
            {
                return false;
            }

            var source = await _db.LeadSyncSources.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.IsActive && s.MarkerName.ToLower() == marker.ToLower(),
                    cancellationToken);
            if (source == null)
            {
                return false;
            }

            var ownerId = await ResolveAndAdvanceNextOwnerAsync(source.Id, cancellationToken);
            if (ownerId == null)
            {
                return false;
            }

            lead.LeadOwnerId = ownerId;
            return true;
        }

        public async Task<int?> PeekNextOwnerIdAsync(int sourceId, CancellationToken cancellationToken = default)
        {
            var assignments = await LoadActiveAssignmentsAsync(sourceId, cancellationToken);
            if (assignments.Count == 0)
            {
                return null;
            }

            var state = await _db.LeadSyncRoundRobinStates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SourceId == sourceId, cancellationToken);
            var index = state?.NextIndex ?? 0;
            var normalized = assignments.Count > 0 ? ((index % assignments.Count) + assignments.Count) % assignments.Count : 0;
            return assignments[normalized].UserId;
        }

        private async Task<int?> ResolveAndAdvanceNextOwnerAsync(
            int sourceId,
            CancellationToken cancellationToken)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            var assignments = await _db.LeadSyncSourceAssignments
                .Where(a => a.SourceId == sourceId)
                .Join(
                    _db.Users.Where(u => u.IsActive),
                    a => a.UserId,
                    u => u.Id,
                    (a, u) => new { a.UserId, a.SortOrder })
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.UserId)
                .ToListAsync(cancellationToken);

            if (assignments.Count == 0)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            var state = await _db.LeadSyncRoundRobinStates
                .FirstOrDefaultAsync(s => s.SourceId == sourceId, cancellationToken);
            if (state == null)
            {
                state = new LeadSyncRoundRobinState
                {
                    SourceId = sourceId,
                    NextIndex = 0,
                    UpdatedAt = DateTime.UtcNow,
                };
                _db.LeadSyncRoundRobinStates.Add(state);
                await _db.SaveChangesAsync(cancellationToken);
            }

            var count = assignments.Count;
            var pickIndex = ((state.NextIndex % count) + count) % count;
            var ownerId = assignments[pickIndex].UserId;

            state.NextIndex = (pickIndex + 1) % count;
            state.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return ownerId;
        }

        private async Task<IReadOnlyList<(int UserId, int SortOrder)>> LoadActiveAssignmentsAsync(
            int sourceId,
            CancellationToken cancellationToken)
        {
            return await _db.LeadSyncSourceAssignments
                .Where(a => a.SourceId == sourceId)
                .Join(
                    _db.Users.Where(u => u.IsActive),
                    a => a.UserId,
                    u => u.Id,
                    (a, u) => new ValueTuple<int, int>(a.UserId, a.SortOrder))
                .OrderBy(x => x.Item2)
                .ThenBy(x => x.Item1)
                .ToListAsync(cancellationToken);
        }
    }
}
