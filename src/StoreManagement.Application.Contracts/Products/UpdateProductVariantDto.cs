using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Products;

public class UpdateProductVariantDto
{
    [MaxLength(ProductVariantConsts.MaxColorLength)]
    public string? Color { get; set; }

    [MaxLength(ProductVariantConsts.MaxSizeLength)]
    public string? Size { get; set; }

    public bool IsActive { get; set; }
}