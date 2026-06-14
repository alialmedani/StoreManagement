using StoreManagement.Common;
using StoreManagement.Products;

namespace StoreManagement.Categories;

public class CategoryStockSummaryRequestDto : StoreManagementPagedAndSortedResultRequestDto
{
    public ProductAvailabilityStatus? AvailabilityStatus { get; set; }

    public CategoryStockProblemType? ProblemType { get; set; }
}