using CRM.DTO;
using CRM.models;

namespace CRM.Helpers
{
    internal static class DealStatusMasterHelper
    {
        public static MasterDataRowDto ToDto(DealStatus row) => new()
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt == default ? null : row.CreatedAt,
            SortOrder = row.SortOrder,
            IsWon = row.IsWon,
            IsLost = row.IsLost,
        };

        public static string? ValidateFlags(bool isWon, bool isLost) =>
            isWon && isLost ? "A deal status cannot be both Won and Lost." : null;

        public static void ApplyUpsert(DealStatus entity, MasterDataUpsertDto dto, int? defaultSortOrder = null)
        {
            entity.Name = (dto.Name ?? string.Empty).Trim();
            entity.Description = dto.Description?.Trim() ?? string.Empty;
            entity.IsActive = dto.IsActive;

            if (dto.SortOrder is int sortOrder && sortOrder > 0)
            {
                entity.SortOrder = sortOrder;
            }
            else if (entity.SortOrder <= 0 && defaultSortOrder is int assigned)
            {
                entity.SortOrder = assigned;
            }

            if (dto.IsWon is bool isWon)
            {
                entity.IsWon = isWon;
            }

            if (dto.IsLost is bool isLost)
            {
                entity.IsLost = isLost;
            }
        }
    }
}
