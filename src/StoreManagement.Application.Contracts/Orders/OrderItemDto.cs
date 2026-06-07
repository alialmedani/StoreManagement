using System;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Orders;

public class OrderItemDto : FullAuditedEntityDto<Guid>
{
    public Guid OrderId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}