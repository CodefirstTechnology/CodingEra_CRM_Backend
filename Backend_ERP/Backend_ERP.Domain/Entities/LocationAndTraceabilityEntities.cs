using System;
using System.Collections.Generic;

namespace Backend_ERP.Domain.Entities
{
    public enum ZoneType
    {
        Bulk,
        PickFace,
        TempControlled,
        Quarantine,
        Staging,
        InTransit
    }

    public enum BinStatus
    {
        Active,
        Blocked,
        CycleCountHold,
        Reserved
    }

    public class WarehouseEntity
    {
        public Guid WarehouseId { get; set; } = Guid.NewGuid();
        public string WarehouseCode { get; set; } = null!;
        public string WarehouseName { get; set; } = null!;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // PostgreSQL xmin concurrency token
        public uint RowVersion { get; set; }

        public ICollection<WarehouseZoneEntity> Zones { get; set; } = new List<WarehouseZoneEntity>();
    }

    public class WarehouseZoneEntity
    {
        public Guid ZoneId { get; set; } = Guid.NewGuid();
        public Guid WarehouseId { get; set; }
        public string ZoneCode { get; set; } = null!;
        public string ZoneName { get; set; } = null!;
        public ZoneType ZoneType { get; set; } = ZoneType.Bulk;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public WarehouseEntity Warehouse { get; set; } = null!;
        public ICollection<WarehouseBinEntity> Bins { get; set; } = new List<WarehouseBinEntity>();
    }

    public class WarehouseBinEntity
    {
        public Guid BinId { get; set; } = Guid.NewGuid();
        public Guid ZoneId { get; set; }
        public Guid WarehouseId { get; set; }
        public string BinCode { get; set; } = null!;
        public decimal MaxWeightKg { get; set; } = 1000.0000m;
        public decimal MaxVolumeM3 { get; set; } = 10.0000m;
        public decimal CurrentWeightKg { get; set; } = 0.0000m;
        public decimal CurrentVolumeM3 { get; set; } = 0.0000m;
        public BinStatus Status { get; set; } = BinStatus.Active;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public WarehouseZoneEntity Zone { get; set; } = null!;
    }

    public class ItemBatchEntity
    {
        public Guid BatchId { get; set; } = Guid.NewGuid();
        public string BatchNumber { get; set; } = null!;
        public Guid ItemId { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime? RetestDate { get; set; }
        public DateTime? BestBeforeDate { get; set; }
        public string QcStatus { get; set; } = "Released";
        public bool IsLocked { get; set; } = false;
        public string? SupplierLotNo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // PostgreSQL xmin concurrency token
        public uint RowVersion { get; set; }
    }
}
