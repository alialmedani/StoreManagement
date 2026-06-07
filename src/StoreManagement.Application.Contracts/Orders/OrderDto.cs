using System;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Orders;

public class OrderDto : FullAuditedEntityDto<Guid>
{
    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string? Note { get; set; }

    public LookupDto Status { get; set; } = new();

    public decimal TotalAmount { get; set; }
}