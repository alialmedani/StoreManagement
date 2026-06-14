using System;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Inventory;

public class StockMovementDto : FullAuditedEntityDto<Guid>
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public Guid ProductVariantId { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Current stock of the variant at the time of reading the API.
    /// </summary>
    public int CurrentStockQuantity { get; set; }

    public LookupDto MovementType { get; set; } = new();

    /// <summary>
    /// Positive for inbound stock and negative for outbound stock.
    /// </summary>
    public int QuantityChange { get; set; }

    public int OldQuantity { get; set; }

    public int NewQuantity { get; set; }

    public LookupDto SourceType { get; set; } = new();

    /// <summary>
    /// OrderId, ProductVariantId or another source entity identifier.
    /// </summary>
    public Guid? ReferenceId { get; set; }

    public string? Note { get; set; }
}