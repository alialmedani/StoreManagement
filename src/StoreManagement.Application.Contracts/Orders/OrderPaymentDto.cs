using System;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Orders;

public class OrderPaymentDto : CreationAuditedEntityDto<Guid>
{
    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public LookupDto PaymentMethod { get; set; } = new();

    public DateTime PaymentDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Note { get; set; }
}