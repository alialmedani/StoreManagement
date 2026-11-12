using System;
using StoreManagement.Common;

namespace StoreManagement.Reports;

public class ReportStockMovementDto
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductVariantName { get; set; } = string.Empty;

    public int OldQuantity { get; set; }

    public int Change { get; set; }

    public int NewQuantity { get; set; }

    public LookupDto Reason { get; set; } = new();

    public LookupDto SourceType { get; set; } = new();

    /// <summary>
    /// Related entity identifier (e.g. the order that produced the movement).
    /// </summary>
    public Guid? ReferenceId { get; set; }

    public string? Note { get; set; }

    public bool IsProductDeleted { get; set; }

    public bool IsVariantDeleted { get; set; }
}
