using StoreManagement.Common;

namespace StoreManagement.Products;

public static class ProductAvailabilityCalculator
{
    public const int DefaultLowStockThreshold = 5;

    public static int NormalizeLowStockThreshold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultLowStockThreshold;
        }

        return int.TryParse(value, out var result) && result >= 0
            ? result
            : DefaultLowStockThreshold;
    }

    public static ProductAvailabilityStatus CalculateStatus(
        int stockQuantity,
        int lowStockThreshold)
    {
        if (stockQuantity <= 0)
        {
            return ProductAvailabilityStatus.OutOfStock;
        }

        if (stockQuantity <= lowStockThreshold)
        {
            return ProductAvailabilityStatus.LowStock;
        }

        return ProductAvailabilityStatus.InStock;
    }

    public static LookupDto CreateStatus(
        int stockQuantity,
        int lowStockThreshold)
    {
        var status = CalculateStatus(stockQuantity, lowStockThreshold);

        return new LookupDto
        {
            Id = (int)status,
            Name = status.ToString()
        };
    }
}