using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Products;

public class GenerateProductVariantsDto
{
    [Required]
    public Guid ProductId { get; set; }

    public List<string>? Colors { get; set; }

    public List<string>? Sizes { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultStockQuantity { get; set; }

    public bool SkipExisting { get; set; } = false;

    public bool IsActive { get; set; } = true;
}