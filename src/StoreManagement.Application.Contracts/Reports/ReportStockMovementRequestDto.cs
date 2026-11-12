using System;
using StoreManagement.Common;
using StoreManagement.Inventory;

namespace StoreManagement.Reports;

public class ReportStockMovementRequestDto :
    StoreManagementPagedAndSortedResultRequestDto
{
    /// <summary>
    /// Inclusive start date. Time part is ignored.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Inclusive end date. Time part is ignored.
    /// </summary>
    public DateTime? ToDate { get; set; }

    public Guid? ProductVariantId { get; set; }

    public StockMovementType? MovementType { get; set; }

    /// <summary>
    /// Unlike ABP's usual default-false soft-delete behaviour, this defaults to TRUE
    /// so historical totals stay stable even after a product/variant is soft-deleted.
    /// Set to false to exclude rows tied to soft-deleted catalog records.
    /// </summary>
    public bool IncludeDeleted { get; set; } = true;
}
