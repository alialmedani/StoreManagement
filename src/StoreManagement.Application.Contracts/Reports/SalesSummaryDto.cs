using System.Collections.Generic;

namespace StoreManagement.Reports;

public class SalesSummaryDto
{
    public decimal TotalRevenue { get; set; }

    public decimal TotalCollected { get; set; }

    public int TotalOrders { get; set; }

    public List<SalesSummaryBucketDto> Buckets { get; set; } = new();

    public List<SalesSummaryStatusBreakdownDto> ByStatus { get; set; } = new();

    public List<SalesSummaryPaymentMethodBreakdownDto> ByPaymentMethod { get; set; } = new();
}
