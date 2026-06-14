using System;
using StoreManagement.Common;

namespace StoreManagement.Products;

public class ProductVariantPagedAndSortedResultRequestDto : StoreManagementPagedAndSortedResultRequestDto
{
    public Guid? CategoryId { get; set; }

    public ProductAvailabilityStatus? AvailabilityStatus { get; set; }
}