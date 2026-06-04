using System;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Products;

public class ProductVariantDto : FullAuditedEntityDto<Guid>
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }
}