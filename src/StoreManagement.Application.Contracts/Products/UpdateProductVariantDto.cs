using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Products;

public class UpdateProductVariantDto
{
    [MaxLength(ProductVariantConsts.MaxColorLength)]
    public string? Color { get; set; }

    [MaxLength(ProductVariantConsts.MaxSizeLength)]
    public string? Size { get; set; }

    /// <summary>
    /// Optional unit cost used for profit/margin reporting.
    /// </summary>
    [Range(0, (double)decimal.MaxValue)]
    public decimal? CostPrice { get; set; }

    public bool IsActive { get; set; }
}