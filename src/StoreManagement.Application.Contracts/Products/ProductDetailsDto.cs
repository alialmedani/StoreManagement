using System.Collections.Generic;

namespace StoreManagement.Products;

public class ProductDetailsDto : ProductDto
{
    public List<ProductVariantSummaryDto> Variants { get; set; } = new();
}