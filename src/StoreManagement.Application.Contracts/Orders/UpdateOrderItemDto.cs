using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Orders;

public class UpdateOrderItemDto
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}