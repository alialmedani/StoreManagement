using System;

namespace StoreManagement.Reports;

public class TopSellerItemDto
{
    public Guid ProductId { get; set; }

    /// <summary>
    /// Populated only when the report is grouped by variant.
    /// </summary>
    public Guid? ProductVariantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public string? Size { get; set; }

    public int QuantitySold { get; set; }

    public decimal Revenue { get; set; }

    public bool IsProductDeleted { get; set; }

    /// <summary>
    /// Only meaningful when the report is grouped by variant; false otherwise.
    /// </summary>
    public bool IsVariantDeleted { get; set; }
}
