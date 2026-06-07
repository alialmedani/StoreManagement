using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Orders;

public class UpdateOrderDto
{
    [Required]
    [MaxLength(OrderConsts.MaxCustomerNameLength)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(OrderConsts.MaxCustomerPhoneLength)]
    public string? CustomerPhone { get; set; }

    [MaxLength(OrderConsts.MaxNoteLength)]
    public string? Note { get; set; }
}