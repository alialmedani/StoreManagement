using System;
using System.Collections.Generic;

namespace StoreManagement.Products;

public class ProductVariantOptionsDto
{
    public Guid ProductId { get; set; }

    public bool RequiresVariantSelection { get; set; }

    public Guid? DefaultVariantId { get; set; }

    public bool HasColorOptions { get; set; }

    public bool HasSizeOptions { get; set; }

    public List<string> Colors { get; set; } = new();

    public List<string> Sizes { get; set; } = new();

    public List<ProductVariantOptionItemDto> Variants { get; set; } = new();
}

public class ProductVariantOptionItemDto
{
    public Guid Id { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public bool IsAvailable { get; set; }
}