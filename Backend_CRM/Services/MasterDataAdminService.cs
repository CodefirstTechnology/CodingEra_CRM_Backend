using CRM.DATA;
using CRM.DTO;
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
                "lead-statuses" => ListCore(_context.LeadStatuses, activeOnly, ct),
                "deal-statuses" => ListCore(_context.DealStatuses, activeOnly, ct),
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
                "lead-statuses" => GetByIdCore(_context.LeadStatuses, id, ct),
                "deal-statuses" => GetByIdCore(_context.DealStatuses, id, ct),
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
                "lead-statuses" => CreateCore(_context.LeadStatuses, () => new LeadStatus(), dto, ct),
                "deal-statuses" => CreateCore(_context.DealStatuses, () => new DealStatus(), dto, ct),
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
                "lead-statuses" => UpdateCore(_context.LeadStatuses, id, dto, ct),
                "deal-statuses" => UpdateCore(_context.DealStatuses, id, dto, ct),
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
                "deal-statuses" => PatchActiveCore(_context.DealStatuses, id, isActive, ct),
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
