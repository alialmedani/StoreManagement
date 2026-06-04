using System;
using StoreManagement.Common;

namespace StoreManagement.Inventory;

public class StockMovementPagedRequestDto : StoreManagementPagedAndSortedResultRequestDto
{
    public Guid? ProductVariantId { get; set; }

    public StockMovementType? MovementType { get; set; }

    public StockMovementSourceType? SourceType { get; set; }

    public Guid? ReferenceId { get; set; }
}