using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Orders;

public class CancelOrderDto
{
    [Required]
    [StringLength(OrderConsts.MaxNoteLength)]
    public string CancellationReason { get; set; } = string.Empty;
}