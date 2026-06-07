using System.Collections.Generic;

namespace StoreManagement.Orders;

public class OrderDetailsDto : OrderDto
{
    public List<OrderItemDto> Items { get; set; } = new();
}