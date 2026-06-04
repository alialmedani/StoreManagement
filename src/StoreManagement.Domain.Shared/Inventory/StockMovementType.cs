namespace StoreManagement.Inventory;

public enum StockMovementType
{
    Increase = 1,
    Decrease = 2,
    Adjustment = 3,
    Sale = 4,
    OrderCancellation = 5,
    Return = 6,
    OpeningStock = 7
}