namespace CRM.DTO
{
    public class ItemGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; }
        public int ChildCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ItemGroupUpsertDto
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ItemAttributeValueDto
    {
        public int Id { get; set; }
        public int AttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class ItemAttributeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ValueType { get; set; } = "Text";
        public bool IsVariantAttribute { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public List<ItemAttributeValueDto> Values { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ItemAttributeUpsertDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ValueType { get; set; } = "Text";
        public bool IsVariantAttribute { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public List<ItemAttributeValueUpsertDto> Values { get; set; } = new();
    }

    public class ItemAttributeValueUpsertDto
    {
        public int? Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ItemSpecificationDto
    {
        public int Id { get; set; }
        public string SpecName { get; set; } = string.Empty;
        public string SpecValue { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class ItemSpecificationUpsertDto
    {
        public int? Id { get; set; }
        public string SpecName { get; set; } = string.Empty;
        public string SpecValue { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class ItemVariantAttributeDto
    {
        public int AttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeCode { get; set; } = string.Empty;
        public int? AttributeValueId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public class ItemVariantAttributeUpsertDto
    {
        public int AttributeId { get; set; }
        public int? AttributeValueId { get; set; }
        public string CustomValue { get; set; } = string.Empty;
    }

    public class ItemListItemDto
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int? ItemGroupId { get; set; }
        public string ItemGroupName { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public bool HasVariants { get; set; }
        public int? ParentItemId { get; set; }
        public string ParentItemName { get; set; } = string.Empty;
        public int VariantCount { get; set; }
        public List<ItemVariantAttributeDto> VariantAttributes { get; set; } = new();
        public decimal SteelRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ItemDetailDto : ItemListItemDto
    {
        public string Description { get; set; } = string.Empty;
        public List<ItemSpecificationDto> Specifications { get; set; } = new();
        public List<ItemVariantAttributeDto> TemplateAttributes { get; set; } = new();
        public List<ItemListItemDto> Variants { get; set; } = new();
    }

    public class ItemUpsertDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int? ItemGroupId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal SteelRate { get; set; }
        public string Status { get; set; } = "Active";
        public bool HasVariants { get; set; }
        public List<int> VariantAttributeIds { get; set; } = new();
        public List<ItemSpecificationUpsertDto> Specifications { get; set; } = new();
    }

    public class ItemVariantUpsertDto
    {
        public string? ItemCode { get; set; }
        public decimal? SteelRate { get; set; }
        public string Status { get; set; } = "Active";
        public List<ItemVariantAttributeUpsertDto> Attributes { get; set; } = new();
        public List<ItemSpecificationUpsertDto> Specifications { get; set; } = new();
    }

    public class ItemVariantGenerateDto
    {
        public List<ItemVariantGenerateAttributeDto> Attributes { get; set; } = new();
        public string Status { get; set; } = "Active";
        public bool SkipExisting { get; set; } = true;
    }

    public class ItemVariantGenerateAttributeDto
    {
        public int AttributeId { get; set; }
        public List<string> Values { get; set; } = new();
    }

    public class ItemListQueryDto
    {
        public string? Search { get; set; }
        public int? ItemGroupId { get; set; }
        public string? Status { get; set; }
        public int? ParentItemId { get; set; }
        public bool IncludeVariants { get; set; }
        public string SortBy { get; set; } = "itemName";
        public string SortDir { get; set; } = "asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Dictionary<string, string>? AttributeFilters { get; set; }
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
