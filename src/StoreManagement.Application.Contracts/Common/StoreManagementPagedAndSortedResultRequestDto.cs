using Volo.Abp.Application.Dtos;

namespace StoreManagement.Common;

public class StoreManagementPagedAndSortedResultRequestDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}