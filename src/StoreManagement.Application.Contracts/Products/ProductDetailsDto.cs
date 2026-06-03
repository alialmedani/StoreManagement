using System.Collections.Generic;

namespace StoreManagement.Products;

public class ProductDetailsDto : ProductDto
{
    public List<string> AvailableColors { get; set; } = new();

    public List<string> AvailableSizes { get; set; } = new();

    public List<ProductVariantSummaryDto> Variants { get; set; } = new();
}