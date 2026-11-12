using System;

namespace StoreManagement.Reports;

public class ProfitMarginItemDto
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

    /// <summary>
    /// Historical: sum of quantity * CostPriceAtSale (the per-sale cost snapshot).
    /// Rows missing a snapshot contribute 0, so this can understate true cost
    /// when <see cref="HasCostData"/> is false.
    /// </summary>
    public decimal CostOfGoodsSold { get; set; }

    public decimal GrossProfit { get; set; }

    public decimal MarginPercent { get; set; }

    /// <summary>
    /// False when one or more sold units are missing a cost snapshot; in that case
    /// CostOfGoodsSold/GrossProfit/MarginPercent understate the true cost.
    /// </summary>
    public bool HasCostData { get; set; }

    public bool IsProductDeleted { get; set; }

    /// <summary>
    /// Only meaningful when the report is grouped by variant; false otherwise.
    /// </summary>
    public bool IsVariantDeleted { get; set; }

    /// <summary>
    /// Informational comparison only: sum of quantity * the variant's CURRENT
    /// CostPrice. Null unless every sold unit in the group has a current cost.
    /// Never affects CostOfGoodsSold/GrossProfit/MarginPercent, which stay historical.
    /// </summary>
    public decimal? CurrentCostOfGoodsSold { get; set; }

    /// <summary>
    /// True if any sold unit has both a cost snapshot and a current cost, and
    /// they differ.
    /// </summary>
    public bool HasCostChangedSinceSale { get; set; }
}
