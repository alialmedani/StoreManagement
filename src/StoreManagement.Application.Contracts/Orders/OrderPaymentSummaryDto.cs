using System;
using StoreManagement.Common;

namespace StoreManagement.Orders;

public class OrderPaymentSummaryDto
{
    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public LookupDto PaymentStatus { get; set; } = new();

    public int PaymentCount { get; set; }

    public DateTime? LastPaymentDate { get; set; }
}