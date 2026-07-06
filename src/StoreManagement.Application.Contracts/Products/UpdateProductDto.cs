using System;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Products;

public class UpdateProductDto
{
    [Required]
    [MaxLength(ProductConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(ProductConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public ProductTargetAudience TargetAudience { get; set; }

    public string? ImageUrl { get; set; }

    public Guid CategoryId { get; set; }
}