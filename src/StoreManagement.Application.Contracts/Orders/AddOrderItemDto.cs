using System;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Orders;

public class AddOrderItemDto
{
    [Required]
    public Guid ProductVariantId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}