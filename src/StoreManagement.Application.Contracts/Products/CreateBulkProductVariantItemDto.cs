using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Products;

public class CreateBulkProductVariantItemDto
{
    [MaxLength(ProductVariantConsts.MaxColorLength)]
    public string? Color { get; set; }

    [MaxLength(ProductVariantConsts.MaxSizeLength)]
    public string? Size { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}