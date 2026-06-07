using CRM.DTO;

namespace CRM.Services
{
    public interface IItemMasterService
    {
        Task<List<ItemGroupDto>> ListGroupsAsync(bool activeOnly = false, CancellationToken ct = default);
        Task<ItemGroupDto?> GetGroupAsync(int id, CancellationToken ct = default);
        Task<ItemGroupDto> CreateGroupAsync(ItemGroupUpsertDto dto, CancellationToken ct = default);
        Task<ItemGroupDto?> UpdateGroupAsync(int id, ItemGroupUpsertDto dto, CancellationToken ct = default);
        Task<bool> DeleteGroupAsync(int id, CancellationToken ct = default);

        Task<List<ItemAttributeDto>> ListAttributesAsync(bool activeOnly = false, CancellationToken ct = default);
        Task<ItemAttributeDto?> GetAttributeAsync(int id, CancellationToken ct = default);
        Task<ItemAttributeDto> CreateAttributeAsync(ItemAttributeUpsertDto dto, CancellationToken ct = default);
        Task<ItemAttributeDto?> UpdateAttributeAsync(int id, ItemAttributeUpsertDto dto, CancellationToken ct = default);
        Task<bool> DeleteAttributeAsync(int id, CancellationToken ct = default);

        Task<PagedResultDto<ItemListItemDto>> ListItemsAsync(ItemListQueryDto query, CancellationToken ct = default);
        Task<ItemDetailDto?> GetItemAsync(int id, CancellationToken ct = default);
        Task<ItemDetailDto> CreateItemAsync(ItemUpsertDto dto, CancellationToken ct = default);
        Task<ItemDetailDto?> UpdateItemAsync(int id, ItemUpsertDto dto, CancellationToken ct = default);
        Task<bool> DeleteItemAsync(int id, CancellationToken ct = default);

        Task<ItemDetailDto?> CreateVariantAsync(int parentId, ItemVariantUpsertDto dto, CancellationToken ct = default);
        Task<ItemDetailDto?> GenerateVariantsAsync(int parentId, ItemVariantGenerateDto dto, CancellationToken ct = default);
        Task<bool> DeleteVariantAsync(int parentId, int variantId, CancellationToken ct = default);
    }
}
