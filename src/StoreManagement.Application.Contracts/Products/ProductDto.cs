using System;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Products;

public class ProductDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int TotalStockQuantity { get; set; }

    public LookupDto AvailabilityStatus { get; set; } = new();

    public bool IsActive { get; set; }

    public EntityLookupDto Category { get; set; } = new();

    public LookupDto TargetAudience { get; set; } = new();
}