using System;
using System.Collections.Generic;

namespace StoreManagement.Products;

public static class ProductVariantSizeConsts
{
    public const string OneSize = "One Size";

    public static readonly HashSet<string> ClothingSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        "XS",
        "S",
        "M",
        "L",
        "XL",
        "XXL",
        "XXXL",
        "2XL",
        "3XL",
        "4XL"
    };

    public static readonly Dictionary<string, int> ClothingSizeSortOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["XS"] = 10,
        ["S"] = 20,
        ["M"] = 30,
        ["L"] = 40,
        ["XL"] = 50,
        ["XXL"] = 60,
        ["XXXL"] = 70,
        ["2XL"] = 80,
        ["3XL"] = 90,
        ["4XL"] = 100
    };

    public static bool IsValidClothingSize(string size)
    {
        return ClothingSizes.Contains(size);
    }

    public static int GetSortOrder(string? size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return int.MaxValue;
        }

        if (ClothingSizeSortOrder.TryGetValue(size, out var clothingOrder))
        {
            return clothingOrder;
        }

        if (int.TryParse(size, out var numericSize))
        {
            return 1000 + numericSize;
        }

        return int.MaxValue - 1;
    }
}