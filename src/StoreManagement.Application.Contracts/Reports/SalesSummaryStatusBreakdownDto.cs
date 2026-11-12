using StoreManagement.Orders;

namespace StoreManagement.Reports;

public class SalesSummaryStatusBreakdownDto
{
    public OrderStatus Status { get; set; }

    public int OrderCount { get; set; }

    public decimal Revenue { get; set; }
}
