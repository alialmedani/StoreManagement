using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Products;

public class CreateBulkProductVariantsDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateBulkProductVariantItemDto> Variants { get; set; } = new();
}