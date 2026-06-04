using System;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Inventory;

public class StockMovementDto : FullAuditedEntityDto<Guid>
{
    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public LookupDto MovementType { get; set; } = new();

    public int QuantityChange { get; set; }

    public int OldQuantity { get; set; }

    public int NewQuantity { get; set; }

    public LookupDto SourceType { get; set; } = new();

    public Guid? ReferenceId { get; set; }

    public string? Note { get; set; }
}