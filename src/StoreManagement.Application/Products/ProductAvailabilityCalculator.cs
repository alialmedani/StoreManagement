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

    public static LookupDto CreateStatus(
        int stockQuantity,
        int lowStockThreshold)
    {
        if (stockQuantity <= 0)
        {
            return new LookupDto
            {
                Id = (int)ProductAvailabilityStatus.OutOfStock,
                Name = ProductAvailabilityStatus.OutOfStock.ToString()
            };
        }

        if (stockQuantity <= lowStockThreshold)
        {
            return new LookupDto
            {
                Id = (int)ProductAvailabilityStatus.LowStock,
                Name = ProductAvailabilityStatus.LowStock.ToString()
            };
        }

        return new LookupDto
        {
            Id = (int)ProductAvailabilityStatus.InStock,
            Name = ProductAvailabilityStatus.InStock.ToString()
        };
    }
}