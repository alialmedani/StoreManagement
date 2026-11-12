using System.Collections.Generic;

namespace StoreManagement.Reports;

public class TopSellersDto
{
    public List<TopSellerItemDto> Items { get; set; } = new();
}
