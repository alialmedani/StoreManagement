using StoreManagement.Orders;

namespace StoreManagement.Reports;

public class SalesSummaryPaymentMethodBreakdownDto
{
    public PaymentMethod PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public int PaymentCount { get; set; }
}
