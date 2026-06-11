namespace StoreManagement.Products;

public class ProductDashboardSummaryDto
{
    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int InactiveProducts { get; set; }

    public int TotalVariants { get; set; }

    public int ActiveVariants { get; set; }

    public int InactiveVariants { get; set; }

    public int TotalStockQuantity { get; set; }

    public int LowStockThreshold { get; set; }

    public int OutOfStockProducts { get; set; }

    public int LowStockProducts { get; set; }

    public int InStockProducts { get; set; }

    public int OutOfStockVariants { get; set; }

    public int LowStockVariants { get; set; }

    public int InStockVariants { get; set; }
}