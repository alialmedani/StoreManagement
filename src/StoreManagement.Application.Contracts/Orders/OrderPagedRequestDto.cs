using StoreManagement.Common;

namespace StoreManagement.Orders;

public class OrderPagedRequestDto : StoreManagementPagedAndSortedResultRequestDto
{
    public OrderStatus? Status { get; set; }
}