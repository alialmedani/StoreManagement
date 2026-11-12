using System;
using StoreManagement.Orders;

namespace StoreManagement.Reports;

public class SalesSummaryRequestDto
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public SalesSummaryGroupBy GroupBy { get; set; } = SalesSummaryGroupBy.Day;

    public OrderStatus? Status { get; set; }

    public OrderPaymentStatus? PaymentStatus { get; set; }
}
