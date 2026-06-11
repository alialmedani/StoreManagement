using StoreManagement.Common;

namespace StoreManagement.Products;

public class ProductPagedAndSortedResultRequestDto : StoreManagementPagedAndSortedResultRequestDto
{
    public ProductAvailabilityStatus? AvailabilityStatus { get; set; }
}