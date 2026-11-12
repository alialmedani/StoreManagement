using System.Collections.Generic;

namespace StoreManagement.Reports;

public class ProfitMarginDto
{
    public List<ProfitMarginItemDto> Items { get; set; } = new();

    public decimal TotalRevenue { get; set; }

    public decimal TotalCostOfGoodsSold { get; set; }

    public decimal TotalGrossProfit { get; set; }

    public decimal TotalMarginPercent { get; set; }

    /// <summary>
    /// False when any sold variant in the range is missing cost data.
    /// </summary>
    public bool HasCompleteCostData { get; set; }

    /// <summary>
    /// Informational comparison only: sum of item CurrentCostOfGoodsSold.
    /// Null unless every item has it. Never affects the Total* historical fields.
    /// </summary>
    public decimal? TotalCurrentCostOfGoodsSold { get; set; }

    public bool HasAnyCostChangedSinceSale { get; set; }
}
