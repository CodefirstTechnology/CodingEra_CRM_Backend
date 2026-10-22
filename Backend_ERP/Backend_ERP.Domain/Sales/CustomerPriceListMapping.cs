using System;
using NpgsqlTypes;

namespace ERP.Domain.Sales
{
    public class CustomerPriceListMapping
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public int PriceListId { get; set; }

        public PriceList PriceList { get; set; } = null!;

        public int PriorityWeight { get; set; } = 100;

        public NpgsqlRange<DateOnly> EffectiveRange { get; set; }

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? UpdatedBy { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
