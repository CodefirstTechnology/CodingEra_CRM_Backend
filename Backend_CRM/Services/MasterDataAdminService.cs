using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface IMasterDataAdminService
    {
        bool IsSupportedEntity(string entity);

        Task<IReadOnlyList<MasterDataRowDto>> ListAsync(string entity, bool activeOnly, CancellationToken ct = default);

        Task<MasterDataRowDto?> GetByIdAsync(string entity, int id, CancellationToken ct = default);

        Task<(MasterDataRowDto? Row, string? Error)> CreateAsync(
            string entity,
            MasterDataUpsertDto dto,
            CancellationToken ct = default);

        Task<(MasterDataRowDto? Row, string? Error, bool NotFound)> UpdateAsync(
            string entity,
            int id,
            MasterDataUpsertDto dto,
            CancellationToken ct = default);

        Task<(MasterDataRowDto? Row, bool NotFound)> PatchActiveAsync(
            string entity,
            int id,
            bool isActive,
            CancellationToken ct = default);

        Task<(IReadOnlyList<MasterDataRowDto>? Rows, string? Error)> ReorderDealStatusesAsync(
            DealStatusReorderDto dto,
            CancellationToken ct = default);
    }

    public sealed class MasterDataAdminService : IMasterDataAdminService
    {
        private static readonly HashSet<string> SupportedEntities = new(StringComparer.OrdinalIgnoreCase)
        {
            "salutations",
            "lead-statuses",
            "deal-statuses",
            "request-types",
            "industries",
            "employee-counts",
            "territories",
        };

        private readonly TaskDbcontext _context;

        public MasterDataAdminService(TaskDbcontext context)
        {
            _context = context;
        }

        public bool IsSupportedEntity(string entity) =>
            SupportedEntities.Contains(entity.Trim());

        public Task<IReadOnlyList<MasterDataRowDto>> ListAsync(string entity, bool activeOnly, CancellationToken ct = default) =>
            entity.Trim().ToLowerInvariant() switch
            {
                "salutations" => ListCore(_context.Salutations, activeOnly, ct),
                "lead-statuses" => ListLeadStatusesAsync(activeOnly, ct),
                "deal-statuses" => ListDealStatusesAsync(activeOnly, ct),
                "request-types" => ListCore(_context.RequestTypes, activeOnly, ct),
                "industries" => ListCore(_context.Industries, activeOnly, ct),
                "employee-counts" => ListCore(_context.EmployeeCounts, activeOnly, ct),
                "territories" => ListCore(_context.Territories, activeOnly, ct),
                _ => throw new ArgumentException($"Unsupported master entity '{entity}'."),
            };

        public Task<MasterDataRowDto?> GetByIdAsync(string entity, int id, CancellationToken ct = default) =>
            entity.Trim().ToLowerInvariant() switch
            {
                "salutations" => GetByIdCore(_context.Salutations, id, ct),
                "lead-statuses" => GetLeadStatusByIdAsync(id, ct),
                "deal-statuses" => GetDealStatusByIdAsync(id, ct),
                "request-types" => GetByIdCore(_context.RequestTypes, id, ct),
                "industries" => GetByIdCore(_context.Industries, id, ct),
                "employee-counts" => GetByIdCore(_context.EmployeeCounts, id, ct),
                "territories" => GetByIdCore(_context.Territories, id, ct),
                _ => throw new ArgumentException($"Unsupported master entity '{entity}'."),
            };

        public Task<(MasterDataRowDto? Row, string? Error)> CreateAsync(
            string entity,
            MasterDataUpsertDto dto,
            CancellationToken ct = default) =>
            entity.Trim().ToLowerInvariant() switch
            {
                "salutations" => CreateCore(_context.Salutations, () => new Salutation(), dto, ct),
                "lead-statuses" => CreateLeadStatusAsync(dto, ct),
                "deal-statuses" => CreateDealStatusAsync(dto, ct),
                "request-types" => CreateCore(_context.RequestTypes, () => new RequestType(), dto, ct),
                "industries" => CreateCore(_context.Industries, () => new Industry(), dto, ct),
                "employee-counts" => CreateCore(_context.EmployeeCounts, () => new EmployeeCount(), dto, ct),
                "territories" => CreateCore(_context.Territories, () => new Territory(), dto, ct),
                _ => throw new ArgumentException($"Unsupported master entity '{entity}'."),
            };

        public Task<(MasterDataRowDto? Row, string? Error, bool NotFound)> UpdateAsync(
            string entity,
            int id,
            MasterDataUpsertDto dto,
            CancellationToken ct = default) =>
            entity.Trim().ToLowerInvariant() switch
            {
                "salutations" => UpdateCore(_context.Salutations, id, dto, ct),
                "lead-statuses" => UpdateLeadStatusAsync(id, dto, ct),
                "deal-statuses" => UpdateDealStatusAsync(id, dto, ct),
                "request-types" => UpdateCore(_context.RequestTypes, id, dto, ct),
                "industries" => UpdateCore(_context.Industries, id, dto, ct),
                "employee-counts" => UpdateCore(_context.EmployeeCounts, id, dto, ct),
                "territories" => UpdateCore(_context.Territories, id, dto, ct),
                _ => throw new ArgumentException($"Unsupported master entity '{entity}'."),
            };

        public Task<(MasterDataRowDto? Row, bool NotFound)> PatchActiveAsync(
            string entity,
            int id,
            bool isActive,
            CancellationToken ct = default) =>
            entity.Trim().ToLowerInvariant() switch
            {
                "salutations" => PatchActiveCore(_context.Salutations, id, isActive, ct),
                "lead-statuses" => PatchActiveCore(_context.LeadStatuses, id, isActive, ct),
                "deal-statuses" => PatchDealStatusActiveAsync(id, isActive, ct),
                "request-types" => PatchActiveCore(_context.RequestTypes, id, isActive, ct),
                "industries" => PatchActiveCore(_context.Industries, id, isActive, ct),
                "employee-counts" => PatchActiveCore(_context.EmployeeCounts, id, isActive, ct),
                "territories" => PatchActiveCore(_context.Territories, id, isActive, ct),
                _ => throw new ArgumentException($"Unsupported master entity '{entity}'."),
            };

        private static MasterDataRowDto ToDto(IMasterDataRow row) => new()
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt == default ? null : row.CreatedAt,
        };

        private static async Task<IReadOnlyList<MasterDataRowDto>> ListCore<T>(
            DbSet<T> set,
            bool activeOnly,
            CancellationToken ct)
            where T : class, IMasterDataRow
        {
            var q = set.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(x => x.IsActive);
            }

            return await q
                .OrderBy(x => x.Name)
                .Select(x => new MasterDataRowDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt == default ? null : x.CreatedAt,
                })
                .ToListAsync(ct);
        }

        private static async Task<MasterDataRowDto?> GetByIdCore<T>(
            DbSet<T> set,
            int id,
            CancellationToken ct)
            where T : class, IMasterDataRow
        {
            var row = await set.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return row == null ? null : ToDto(row);
        }

        private async Task<(MasterDataRowDto? Row, string? Error)> CreateLeadStatusAsync(
            MasterDataUpsertDto dto,
            CancellationToken ct)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return (null, "Name is required.");
            }

            if (await NameExistsAsync(_context.LeadStatuses, name, excludeId: null, ct))
            {
                return (null, "A record with this name already exists.");
            }

            var isConversion =
                dto.IsConversionStatus == true ||
                LeadStatusMovedToDealSeed.IsConversionStatusName(name);

            if (isConversion && await _context.LeadStatuses.AnyAsync(x => x.IsConversionStatus, ct))
            {
                return (null, "Only one lead status can be marked as the lead→deal conversion status.");
            }

            var entity = new LeadStatus
            {
                Id = 0,
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description)
                    ? (isConversion
                        ? "Lead has been converted into a deal (not Won / not revenue)."
                        : string.Empty)
                    : dto.Description.Trim(),
                IsActive = dto.IsActive,
                IsConversionStatus = isConversion,
            };
            await _context.LeadStatuses.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            return (ToLeadStatusDto(entity), null);
        }

        private async Task<(MasterDataRowDto? Row, string? Error, bool NotFound)> UpdateLeadStatusAsync(
            int id,
            MasterDataUpsertDto dto,
            CancellationToken ct)
        {
            if (dto.Id != 0 && dto.Id != id)
            {
                return (null, "Route id and body id must match when the body includes an id.", false);
            }

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return (null, "Name is required.", false);
            }

            var existing = await _context.LeadStatuses.FindAsync([id], ct);
            if (existing == null)
            {
                return (null, null, true);
            }

            if (await NameExistsAsync(_context.LeadStatuses, name, excludeId: id, ct))
            {
                return (null, "A record with this name already exists.", false);
            }

            var isConversion = dto.IsConversionStatus ?? existing.IsConversionStatus;
            if (LeadStatusMovedToDealSeed.IsConversionStatusName(name) && dto.IsConversionStatus != false)
            {
                isConversion = true;
            }

            if (isConversion)
            {
                var others = await _context.LeadStatuses
                    .Where(x => x.IsConversionStatus && x.Id != id)
                    .ToListAsync(ct);
                foreach (var o in others)
                {
                    o.IsConversionStatus = false;
                }
            }

            existing.Name = name;
            existing.Description = dto.Description?.Trim() ?? string.Empty;
            existing.IsActive = dto.IsActive;
            existing.IsConversionStatus = isConversion;
            await _context.SaveChangesAsync(ct);
            return (ToLeadStatusDto(existing), null, false);
        }

        private async Task<IReadOnlyList<MasterDataRowDto>> ListLeadStatusesAsync(bool activeOnly, CancellationToken ct)
        {
            var q = _context.LeadStatuses.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(x => x.IsActive);
            }

            return await q
                .OrderBy(x => x.Name)
                .Select(x => new MasterDataRowDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt == default ? null : x.CreatedAt,
                    IsConversionStatus = x.IsConversionStatus,
                })
                .ToListAsync(ct);
        }

        private async Task<MasterDataRowDto?> GetLeadStatusByIdAsync(int id, CancellationToken ct)
        {
            var row = await _context.LeadStatuses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return row == null ? null : ToLeadStatusDto(row);
        }

        private static MasterDataRowDto ToLeadStatusDto(LeadStatus row) => new()
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt == default ? null : row.CreatedAt,
            IsConversionStatus = row.IsConversionStatus,
        };

        private async Task<(MasterDataRowDto? Row, string? Error)> CreateCore<T>(
            DbSet<T> set,
            Func<T> factory,
            MasterDataUpsertDto dto,
            CancellationToken ct)
            where T : class, IMasterDataRow
        {
            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return (null, "Name is required.");
            }

            if (await NameExistsAsync(set, name, excludeId: null, ct))
            {
                return (null, "A record with this name already exists.");
            }

            var entity = factory();
            entity.Id = 0;
            entity.Name = name;
            entity.Description = dto.Description?.Trim() ?? string.Empty;
            entity.IsActive = dto.IsActive;
            await set.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            return (ToDto(entity), null);
        }

        private async Task<(MasterDataRowDto? Row, string? Error, bool NotFound)> UpdateCore<T>(
            DbSet<T> set,
            int id,
            MasterDataUpsertDto dto,
            CancellationToken ct)
            where T : class, IMasterDataRow
        {
            if (dto.Id != 0 && dto.Id != id)
            {
                return (null, "Route id and body id must match when the body includes an id.", false);
            }

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return (null, "Name is required.", false);
            }

            var existing = await set.FindAsync([id], ct);
            if (existing == null)
            {
                return (null, null, true);
            }

            if (await NameExistsAsync(set, name, excludeId: id, ct))
            {
                return (null, "A record with this name already exists.", false);
            }

            existing.Name = name;
            existing.Description = dto.Description?.Trim() ?? string.Empty;
            existing.IsActive = dto.IsActive;
            await _context.SaveChangesAsync(ct);
            return (ToDto(existing), null, false);
        }

        private async Task<(MasterDataRowDto? Row, bool NotFound)> PatchActiveCore<T>(
            DbSet<T> set,
            int id,
            bool isActive,
            CancellationToken ct)
            where T : class, IMasterDataRow
        {
            var existing = await set.FindAsync([id], ct);
            if (existing == null)
            {
                return (null, true);
            }

            existing.IsActive = isActive;
            await _context.SaveChangesAsync(ct);
            return (ToDto(existing), false);
        }

        public async Task<(IReadOnlyList<MasterDataRowDto>? Rows, string? Error)> ReorderDealStatusesAsync(
            DealStatusReorderDto dto,
            CancellationToken ct = default)
        {
            if (dto.Items == null || dto.Items.Count == 0)
            {
                return (null, "At least one item is required.");
            }

            var ids = dto.Items.Select(i => i.Id).Distinct().ToList();
            var rows = await _context.DealStatuses.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
            if (rows.Count != ids.Count)
            {
                return (null, "One or more deal statuses were not found.");
            }

            foreach (var item in dto.Items)
            {
                var row = rows.First(r => r.Id == item.Id);
                row.SortOrder = item.SortOrder;
            }

            await _context.SaveChangesAsync(ct);
            var ordered = await ListDealStatusesAsync(activeOnly: false, ct);
            return (ordered, null);
        }

        private async Task<IReadOnlyList<MasterDataRowDto>> ListDealStatusesAsync(bool activeOnly, CancellationToken ct)
        {
            var q = _context.DealStatuses.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(x => x.IsActive);
            }

            return await q
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .Select(x => new MasterDataRowDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt == default ? null : x.CreatedAt,
                    SortOrder = x.SortOrder,
                    IsWon = x.IsWon,
                    IsLost = x.IsLost,
                })
                .ToListAsync(ct);
        }

        private async Task<MasterDataRowDto?> GetDealStatusByIdAsync(int id, CancellationToken ct)
        {
            var row = await _context.DealStatuses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return row == null ? null : DealStatusMasterHelper.ToDto(row);
        }

        private async Task<(MasterDataRowDto? Row, string? Error)> CreateDealStatusAsync(
            MasterDataUpsertDto dto,
            CancellationToken ct)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return (null, "Name is required.");
            }

            if (await NameExistsAsync(_context.DealStatuses, name, excludeId: null, ct))
            {
                return (null, "A record with this name already exists.");
            }

            var isWon = dto.IsWon ?? false;
            var isLost = dto.IsLost ?? false;
            var flagErr = DealStatusMasterHelper.ValidateFlags(isWon, isLost);
            if (flagErr != null)
            {
                return (null, flagErr);
            }

            var maxSort = await _context.DealStatuses.MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;
            var entity = new DealStatus { Id = 0 };
            DealStatusMasterHelper.ApplyUpsert(entity, dto, defaultSortOrder: maxSort + 10);
            entity.IsWon = isWon;
            entity.IsLost = isLost;
            await _context.DealStatuses.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            return (DealStatusMasterHelper.ToDto(entity), null);
        }

        private async Task<(MasterDataRowDto? Row, bool NotFound)> PatchDealStatusActiveAsync(
            int id,
            bool isActive,
            CancellationToken ct)
        {
            var existing = await _context.DealStatuses.FindAsync([id], ct);
            if (existing == null)
            {
                return (null, true);
            }

            existing.IsActive = isActive;
            await _context.SaveChangesAsync(ct);
            return (DealStatusMasterHelper.ToDto(existing), false);
        }

        private async Task<(MasterDataRowDto? Row, string? Error, bool NotFound)> UpdateDealStatusAsync(
            int id,
            MasterDataUpsertDto dto,
            CancellationToken ct)
        {
            if (dto.Id != 0 && dto.Id != id)
            {
                return (null, "Route id and body id must match when the body includes an id.", false);
            }

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return (null, "Name is required.", false);
            }

            var existing = await _context.DealStatuses.FindAsync([id], ct);
            if (existing == null)
            {
                return (null, null, true);
            }

            if (await NameExistsAsync(_context.DealStatuses, name, excludeId: id, ct))
            {
                return (null, "A record with this name already exists.", false);
            }

            var isWon = dto.IsWon ?? existing.IsWon;
            var isLost = dto.IsLost ?? existing.IsLost;
            var flagErr = DealStatusMasterHelper.ValidateFlags(isWon, isLost);
            if (flagErr != null)
            {
                return (null, flagErr, false);
            }

            DealStatusMasterHelper.ApplyUpsert(existing, dto);
            existing.IsWon = isWon;
            existing.IsLost = isLost;
            await _context.SaveChangesAsync(ct);
            return (DealStatusMasterHelper.ToDto(existing), null, false);
        }

        private static async Task<bool> NameExistsAsync<T>(
            DbSet<T> set,
            string name,
            int? excludeId,
            CancellationToken ct)
            where T : class, IMasterDataRow
        {
            var normalized = name.ToLower();
            var q = set.AsNoTracking().Where(x => x.Name.ToLower() == normalized);
            if (excludeId.HasValue)
            {
                q = q.Where(x => x.Id != excludeId.Value);
            }

            return await q.AnyAsync(ct);
        }
    }
}
