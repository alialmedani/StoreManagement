using System;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Orders;

public class CreateOrderItemDto
{
    [Required]
    public Guid ProductVariantId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}