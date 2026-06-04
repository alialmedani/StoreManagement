using System;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Inventory;

public class AdjustStockDto
{
    [Required]
    public Guid ProductVariantId { get; set; }

    [Required]
    public StockMovementType MovementType { get; set; }

    /*
     * Used for:
     * Increase
     * Decrease
     * Sale
     * Return
     * OrderCancellation
     *
     * Always send it as positive number.
     * The service will decide if it increases or decreases stock.
     */
    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    /*
     * Used only for Adjustment.
     * Example:
     * Current stock = 20
     * NewQuantity = 15
     * QuantityChange will be -5
     */
    [Range(0, int.MaxValue)]
    public int? NewQuantity { get; set; }

    [MaxLength(InventoryConsts.MaxNoteLength)]
    public string? Note { get; set; }
}