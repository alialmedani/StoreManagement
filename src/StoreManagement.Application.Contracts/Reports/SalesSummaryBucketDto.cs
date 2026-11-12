using System;

namespace StoreManagement.Reports;

public class SalesSummaryBucketDto
{
    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal Revenue { get; set; }

    public int OrderCount { get; set; }
}
