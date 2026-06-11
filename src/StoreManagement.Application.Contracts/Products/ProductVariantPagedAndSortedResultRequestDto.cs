using StoreManagement.Common;

namespace StoreManagement.Products;

public class ProductVariantPagedAndSortedResultRequestDto : StoreManagementPagedAndSortedResultRequestDto
{
    public ProductAvailabilityStatus? AvailabilityStatus { get; set; }
}