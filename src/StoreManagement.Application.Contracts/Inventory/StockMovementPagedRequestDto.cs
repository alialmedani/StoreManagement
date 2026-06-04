using System;
using StoreManagement.Common;

namespace StoreManagement.Inventory;

public class StockMovementPagedRequestDto : StoreManagementPagedAndSortedResultRequestDto
{
    public Guid? ProductVariantId { get; set; }

    public StockMovementType? MovementType { get; set; }
}