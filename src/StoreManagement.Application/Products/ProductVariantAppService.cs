using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StoreManagement.Categories;
using StoreManagement.Common;
using StoreManagement.Inventory;
using StoreManagement.Permissions;
using StoreManagement.Settings;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;

namespace StoreManagement.Products;

public class ProductVariantAppService : ApplicationService, IProductVariantAppService
{
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly InventoryManager _inventoryManager;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;
    private readonly ISettingProvider _settingProvider;

    public ProductVariantAppService(
        IRepository<ProductVariant, Guid> productVariantRepository,
        IRepository<Product, Guid> productRepository,
        InventoryManager inventoryManager,
        IDataFilter<ISoftDelete> softDeleteFilter,
        ISettingProvider settingProvider)
    {
        _productVariantRepository = productVariantRepository;
        _productRepository = productRepository;
        _inventoryManager = inventoryManager;
        _softDeleteFilter = softDeleteFilter;
        _settingProvider = settingProvider;
    }

    public async Task<PagedResultDto<ProductVariantDto>> GetListAsync(StoreManagementPagedAndSortedResultRequestDto input)
    {
        var query = await _productVariantRepository.GetQueryableAsync();

        query = ApplyFilter(query, input.Filter);
        query = ApplySorting(query, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(MapToDtoExpression())
        );

        var lowStockThreshold = await GetLowStockThresholdAsync();

        ApplyAvailabilityStatus(items, lowStockThreshold);

        return new PagedResultDto<ProductVariantDto>(totalCount, items);
    }

    public async Task<PagedResultDto<ProductVariantDto>> GetByProductAsync(
        Guid productId,
        StoreManagementPagedAndSortedResultRequestDto input)
    {
        var query = await _productVariantRepository.GetQueryableAsync();

        query = query.Where(variant => variant.ProductId == productId);

        query = ApplyFilter(query, input.Filter);
        query = ApplySorting(query, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(MapToDtoExpression())
        );

        var lowStockThreshold = await GetLowStockThresholdAsync();

        ApplyAvailabilityStatus(items, lowStockThreshold);

        return new PagedResultDto<ProductVariantDto>(totalCount, items);
    }

    public async Task<PagedResultDto<ProductVariantDto>> GetDeletedListAsync(StoreManagementPagedAndSortedResultRequestDto input)
    {
        using (_softDeleteFilter.Disable())
        {
            var query = await _productVariantRepository.GetQueryableAsync();

            query = query.Where(variant => variant.IsDeleted);

            query = ApplyFilter(query, input.Filter);
            query = ApplySorting(query, input.Sorting);

            var totalCount = await AsyncExecuter.CountAsync(query);

            var items = await AsyncExecuter.ToListAsync(
                query
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
                    .Select(MapToDtoExpression())
            );

            var lowStockThreshold = await GetLowStockThresholdAsync();

            ApplyAvailabilityStatus(items, lowStockThreshold);

            return new PagedResultDto<ProductVariantDto>(totalCount, items);
        }
    }

    public async Task<ProductVariantDto> GetAsync(Guid id)
    {
        var query = await _productVariantRepository.GetQueryableAsync();

        var variant = await AsyncExecuter.FirstOrDefaultAsync(
            query
                .Where(variant => variant.Id == id)
                .Select(MapToDtoExpression())
        );

        if (variant == null)
        {
            throw new EntityNotFoundException(typeof(ProductVariant), id);
        }

        var lowStockThreshold = await GetLowStockThresholdAsync();

        variant.AvailabilityStatus = ProductAvailabilityCalculator.CreateStatus(
            variant.StockQuantity,
            lowStockThreshold
        );

        return variant;
    }

    public async Task<ProductVariantOptionsDto> GetOptionsAsync(Guid productId)
    {
        var productInfo = await GetProductInfoAsync(productId);

        var query = await _productVariantRepository.GetQueryableAsync();

        var variants = await AsyncExecuter.ToListAsync(
            query
                .Where(variant =>
                    variant.ProductId == productId &&
                    variant.IsActive &&
                    variant.Product.IsActive &&
                    variant.Product.Category.IsActive)
                .OrderBy(variant => variant.Color)
                .ThenBy(variant => variant.Size)
                .Select(variant => new ProductVariantOptionItemDto
                {
                    Id = variant.Id,
                    Color = variant.Color == ProductVariantConsts.NoColor
                        ? null
                        : variant.Color,
                    Size = variant.Size == ProductVariantConsts.NoSize ||
                           variant.Size == ProductVariantSizeConsts.OneSize
                        ? null
                        : variant.Size,
                    StockQuantity = variant.StockQuantity,
                    IsActive = variant.IsActive,
                    IsAvailable = variant.IsActive && variant.StockQuantity > 0
                })
        );

        variants = variants
            .OrderBy(variant => variant.Color ?? string.Empty)
            .ThenBy(variant => ProductVariantSizeConsts.GetSortOrder(variant.Size))
            .ThenBy(variant => variant.Size ?? string.Empty)
            .ToList();

        var availableVariants = variants
            .Where(variant => variant.IsAvailable)
            .ToList();

        var colors = availableVariants
            .Where(variant => !string.IsNullOrWhiteSpace(variant.Color))
            .Select(variant => variant.Color!)
            .Distinct()
            .OrderBy(color => color)
            .ToList();

        var sizes = availableVariants
            .Where(variant => !string.IsNullOrWhiteSpace(variant.Size))
            .Select(variant => variant.Size!)
            .Distinct()
            .OrderBy(ProductVariantSizeConsts.GetSortOrder)
            .ThenBy(size => size)
            .ToList();

        var hasColorOptions = colors.Count > 0;
        var hasSizeOptions = sizes.Count > 0;

        var requiresVariantSelection =
            hasColorOptions ||
            hasSizeOptions ||
            availableVariants.Count > 1;

        Guid? defaultVariantId = null;

        if (!requiresVariantSelection && availableVariants.Count == 1)
        {
            defaultVariantId = availableVariants[0].Id;
        }

        return new ProductVariantOptionsDto
        {
            ProductId = productInfo.ProductId,
            RequiresVariantSelection = requiresVariantSelection,
            DefaultVariantId = defaultVariantId,
            HasColorOptions = hasColorOptions,
            HasSizeOptions = hasSizeOptions,
            Colors = colors,
            Sizes = sizes,
            Variants = variants
        };
    }

    [Authorize(StoreManagementPermissions.ProductVariants.Create)]
    public async Task<ProductVariantDto> CreateAsync(CreateProductVariantDto input)
    {
        var productInfo = await GetProductInfoAsync(input.ProductId);

        var color = NormalizeColor(input.Color);
        var size = NormalizeSizeByCategory(input.Size, productInfo.SizeType);

        await EnsureVariantIsUniqueAsync(
            input.ProductId,
            NormalizeKey(color),
            NormalizeKey(size)
        );

        var variant = new ProductVariant(
            GuidGenerator.Create(),
            input.ProductId,
            color,
            size,
            input.StockQuantity,
            input.IsActive
        );

        await _productVariantRepository.InsertAsync(variant, autoSave: true);

        await CreateOpeningStockMovementIfNeededAsync(variant);

        return await GetAsync(variant.Id);
    }

    [Authorize(StoreManagementPermissions.ProductVariants.Create)]
    public async Task<List<ProductVariantDto>> BulkCreateAsync(CreateBulkProductVariantsDto input)
    {
        var productInfo = await GetProductInfoAsync(input.ProductId);

        var normalizedItems = input.Variants
            .Select(item =>
            {
                var color = NormalizeColor(item.Color);
                var size = NormalizeSizeByCategory(item.Size, productInfo.SizeType);

                return new NormalizedVariantItem(
                    color,
                    size,
                    NormalizeKey(color),
                    NormalizeKey(size),
                    item.StockQuantity,
                    item.IsActive
                );
            })
            .ToList();

        EnsureRequestHasNoDuplicates(normalizedItems);

        await EnsureNoExistingDuplicatesAsync(input.ProductId, normalizedItems);

        var variants = normalizedItems
            .Select(item => new ProductVariant(
                GuidGenerator.Create(),
                input.ProductId,
                item.Color,
                item.Size,
                item.StockQuantity,
                item.IsActive
            ))
            .ToList();

        await _productVariantRepository.InsertManyAsync(variants, autoSave: true);

        foreach (var variant in variants)
        {
            await CreateOpeningStockMovementIfNeededAsync(variant);
        }

        return await GetByIdsAsync(variants.Select(variant => variant.Id).ToList());
    }

    [Authorize(StoreManagementPermissions.ProductVariants.Create)]
    public async Task<List<ProductVariantDto>> GenerateAsync(GenerateProductVariantsDto input)
    {
        var productInfo = await GetProductInfoAsync(input.ProductId);

        var colors = NormalizeGeneratedColors(input.Colors);
        var sizes = NormalizeGeneratedSizes(input.Sizes, productInfo.SizeType);

        var generatedItems = colors
            .SelectMany(color => sizes.Select(size => new NormalizedVariantItem(
                color,
                size,
                NormalizeKey(color),
                NormalizeKey(size),
                input.DefaultStockQuantity,
                input.IsActive
            )))
            .ToList();

        EnsureRequestHasNoDuplicates(generatedItems);

        var existingKeys = await GetExistingVariantKeysAsync(input.ProductId);

        if (!input.SkipExisting && generatedItems.Any(item => existingKeys.Contains(item.Key)))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantAlreadyExists);
        }

        if (input.SkipExisting)
        {
            generatedItems = generatedItems
                .Where(item => !existingKeys.Contains(item.Key))
                .ToList();
        }

        if (generatedItems.Count == 0)
        {
            return new List<ProductVariantDto>();
        }

        var variants = generatedItems
            .Select(item => new ProductVariant(
                GuidGenerator.Create(),
                input.ProductId,
                item.Color,
                item.Size,
                item.StockQuantity,
                item.IsActive
            ))
            .ToList();

        await _productVariantRepository.InsertManyAsync(variants, autoSave: true);

        foreach (var variant in variants)
        {
            await CreateOpeningStockMovementIfNeededAsync(variant);
        }

        return await GetByIdsAsync(variants.Select(variant => variant.Id).ToList());
    }

    [Authorize(StoreManagementPermissions.ProductVariants.Edit)]
    public async Task<ProductVariantDto> UpdateAsync(Guid id, UpdateProductVariantDto input)
    {
        var variant = await _productVariantRepository.GetAsync(id);

        var productInfo = await GetProductInfoAsync(variant.ProductId);

        var color = NormalizeColor(input.Color);
        var size = NormalizeSizeByCategory(input.Size, productInfo.SizeType);

        await EnsureVariantIsUniqueAsync(
            variant.ProductId,
            NormalizeKey(color),
            NormalizeKey(size),
            id
        );

        variant.SetColorAndSize(color, size);
        variant.SetActive(input.IsActive);

        await _productVariantRepository.UpdateAsync(variant, autoSave: true);

        return await GetAsync(variant.Id);
    }

    [Authorize(StoreManagementPermissions.ProductVariants.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var variant = await _productVariantRepository.GetAsync(id);

        if (variant.StockQuantity > 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantHasStock);
        }

        using (_softDeleteFilter.Disable())
        {
            var hasMovements = await _inventoryManager.HasMovementsAsync(variant.Id);

            if (hasMovements)
            {
                throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantHasMovements);
            }
        }

        await _productVariantRepository.DeleteAsync(variant, autoSave: true);
    }

    [Authorize(StoreManagementPermissions.ProductVariants.Restore)]
    public async Task RestoreAsync(Guid id)
    {
        using (_softDeleteFilter.Disable())
        {
            var variant = await _productVariantRepository.FindAsync(id);

            if (variant == null || !variant.IsDeleted)
            {
                throw new EntityNotFoundException(typeof(ProductVariant), id);
            }

            var productExists = await _productRepository.AnyAsync(product =>
                product.Id == variant.ProductId &&
                !product.IsDeleted);

            if (!productExists)
            {
                throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantProductNotFound);
            }

            var duplicateExists = await _productVariantRepository.AnyAsync(otherVariant =>
                otherVariant.Id != variant.Id &&
                !otherVariant.IsDeleted &&
                otherVariant.ProductId == variant.ProductId &&
                otherVariant.NormalizedColor == variant.NormalizedColor &&
                otherVariant.NormalizedSize == variant.NormalizedSize);

            if (duplicateExists)
            {
                throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantCannotRestoreDuplicate);
            }

            variant.Restore();

            await _productVariantRepository.UpdateAsync(variant, autoSave: true);
        }
    }

    private async Task<ProductVariantProductInfo> GetProductInfoAsync(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantProductNotFound);
        }

        var query = await _productRepository.GetQueryableAsync();

        var productInfo = await AsyncExecuter.FirstOrDefaultAsync(
            query
                .Where(product => product.Id == productId)
                .Select(product => new ProductVariantProductInfo(
                    product.Id,
                    product.Name,
                    product.Category.SizeType
                ))
        );

        if (productInfo == null)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantProductNotFound);
        }

        return productInfo;
    }

    private async Task<List<ProductVariantDto>> GetByIdsAsync(List<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return new List<ProductVariantDto>();
        }

        var query = await _productVariantRepository.GetQueryableAsync();

        var items = await AsyncExecuter.ToListAsync(
            query
                .Where(variant => ids.Contains(variant.Id))
                .Select(MapToDtoExpression())
        );

        var lowStockThreshold = await GetLowStockThresholdAsync();

        ApplyAvailabilityStatus(items, lowStockThreshold);

        return items
            .OrderBy(item => item.Color)
            .ThenBy(item => ProductVariantSizeConsts.GetSortOrder(item.Size))
            .ThenBy(item => item.Size)
            .ToList();
    }

    private async Task EnsureVariantIsUniqueAsync(
        Guid productId,
        string normalizedColor,
        string normalizedSize,
        Guid? excludedVariantId = null)
    {
        var duplicateExists = await _productVariantRepository.AnyAsync(variant =>
            variant.ProductId == productId &&
            variant.NormalizedColor == normalizedColor &&
            variant.NormalizedSize == normalizedSize &&
            (!excludedVariantId.HasValue || variant.Id != excludedVariantId.Value));

        if (duplicateExists)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantAlreadyExists);
        }
    }

    private async Task EnsureNoExistingDuplicatesAsync(Guid productId, List<NormalizedVariantItem> normalizedItems)
    {
        var existingKeys = await GetExistingVariantKeysAsync(productId);

        var hasExistingDuplicate = normalizedItems.Any(item => existingKeys.Contains(item.Key));

        if (hasExistingDuplicate)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantAlreadyExists);
        }
    }

    private async Task<HashSet<string>> GetExistingVariantKeysAsync(Guid productId)
    {
        var query = await _productVariantRepository.GetQueryableAsync();

        var existingKeys = await AsyncExecuter.ToListAsync(
            query
                .Where(variant => variant.ProductId == productId)
                .Select(variant => variant.NormalizedColor + "|" + variant.NormalizedSize)
        );

        return existingKeys.ToHashSet();
    }

    private async Task CreateOpeningStockMovementIfNeededAsync(ProductVariant variant)
    {
        await _inventoryManager.CreateOpeningStockAsync(variant);
    }

    private static void EnsureRequestHasNoDuplicates(List<NormalizedVariantItem> items)
    {
        var hasDuplicate = items
            .GroupBy(item => item.Key)
            .Any(group => group.Count() > 1);

        if (hasDuplicate)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantAlreadyExists);
        }
    }

    private static IQueryable<ProductVariant> ApplyFilter(IQueryable<ProductVariant> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return query;
        }

        var normalizedFilter = filter.Trim();

        return query.Where(variant =>
            variant.Color.Contains(normalizedFilter) ||
            variant.Size.Contains(normalizedFilter) ||
            variant.Product.Name.Contains(normalizedFilter));
    }

    private static IQueryable<ProductVariant> ApplySorting(IQueryable<ProductVariant> query, string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            return query.OrderByDescending(variant => variant.CreationTime);
        }

        return sorting.Trim().ToLowerInvariant() switch
        {
            "color" or "color asc" => query.OrderBy(variant => variant.Color),
            "color desc" => query.OrderByDescending(variant => variant.Color),

            "size" or "size asc" => query.OrderBy(variant => variant.Size),
            "size desc" => query.OrderByDescending(variant => variant.Size),

            "stockquantity" or "stockquantity asc" => query.OrderBy(variant => variant.StockQuantity),
            "stockquantity desc" => query.OrderByDescending(variant => variant.StockQuantity),

            "isactive" or "isactive asc" => query.OrderBy(variant => variant.IsActive),
            "isactive desc" => query.OrderByDescending(variant => variant.IsActive),

            "creationtime" or "creationtime desc" => query.OrderByDescending(variant => variant.CreationTime),
            "creationtime asc" => query.OrderBy(variant => variant.CreationTime),

            _ => query.OrderByDescending(variant => variant.CreationTime)
        };
    }

    private static Expression<Func<ProductVariant, ProductVariantDto>> MapToDtoExpression()
    {
        return variant => new ProductVariantDto
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            ProductName = variant.Product.Name,
            Color = variant.Color,
            Size = variant.Size,
            StockQuantity = variant.StockQuantity,
            IsActive = variant.IsActive,
            CreationTime = variant.CreationTime,
            CreatorId = variant.CreatorId,
            LastModificationTime = variant.LastModificationTime,
            LastModifierId = variant.LastModifierId,
            IsDeleted = variant.IsDeleted,
            DeleterId = variant.DeleterId,
            DeletionTime = variant.DeletionTime
        };
    }

    private async Task<int> GetLowStockThresholdAsync()
    {
        var value = await _settingProvider.GetOrNullAsync(StoreManagementSettings.LowStockThreshold);

        return ProductAvailabilityCalculator.NormalizeLowStockThreshold(value);
    }

    private static void ApplyAvailabilityStatus(
        List<ProductVariantDto> items,
        int lowStockThreshold)
    {
        foreach (var item in items)
        {
            item.AvailabilityStatus = ProductAvailabilityCalculator.CreateStatus(
                item.StockQuantity,
                lowStockThreshold
            );
        }
    }

    private static List<string> NormalizeGeneratedColors(List<string>? colors)
    {
        var rawColors = colors?
            .Select(NormalizeText)
            .Where(color => !string.IsNullOrWhiteSpace(color))
            .ToList() ?? new List<string>();

        if (rawColors.Count == 0)
        {
            return new List<string> { ProductVariantConsts.NoColor };
        }

        return rawColors
            .GroupBy(NormalizeKey)
            .Select(group => group.First())
            .ToList();
    }

    private static List<string> NormalizeGeneratedSizes(List<string>? sizes, CategorySizeType sizeType)
    {
        var rawSizes = sizes?
            .Select(NormalizeText)
            .Where(size => !string.IsNullOrWhiteSpace(size))
            .ToList() ?? new List<string>();

        if (sizeType == CategorySizeType.None)
        {
            if (rawSizes.Count == 0)
            {
                return new List<string> { ProductVariantConsts.NoSize };
            }

            var normalizedNoSizeOnly = rawSizes
                .Select(size => NormalizeSizeByCategory(size, sizeType))
                .ToList();

            return normalizedNoSizeOnly
                .GroupBy(NormalizeKey)
                .Select(group => group.First())
                .ToList();
        }

        if (sizeType == CategorySizeType.OneSize)
        {
            if (rawSizes.Count == 0)
            {
                return new List<string> { ProductVariantSizeConsts.OneSize };
            }
        }

        if (rawSizes.Count == 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory);
        }

        return rawSizes
            .Select(size => NormalizeSizeByCategory(size, sizeType))
            .GroupBy(NormalizeKey)
            .Select(group => group.First())
            .ToList();
    }

    private static string NormalizeColor(string? color)
    {
        var normalizedColor = NormalizeText(color);

        return string.IsNullOrWhiteSpace(normalizedColor)
            ? ProductVariantConsts.NoColor
            : normalizedColor;
    }

    private static string NormalizeSizeByCategory(string? size, CategorySizeType sizeType)
    {
        var normalizedSize = NormalizeText(size);

        return sizeType switch
        {
            CategorySizeType.None => string.IsNullOrWhiteSpace(normalizedSize) ||
                                     string.Equals(normalizedSize, ProductVariantConsts.NoSize, StringComparison.OrdinalIgnoreCase)
                ? ProductVariantConsts.NoSize
                : throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory),

            CategorySizeType.OneSize => string.IsNullOrWhiteSpace(normalizedSize) ||
                                        string.Equals(normalizedSize, ProductVariantSizeConsts.OneSize, StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(normalizedSize, "OneSize", StringComparison.OrdinalIgnoreCase)
                ? ProductVariantSizeConsts.OneSize
                : throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory),

            CategorySizeType.Shoes => NormalizeShoeSize(normalizedSize),

            CategorySizeType.Clothing => NormalizeClothingSize(normalizedSize),

            CategorySizeType.KidsAge => string.IsNullOrWhiteSpace(normalizedSize)
                ? throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory)
                : normalizedSize,

            CategorySizeType.Custom => string.IsNullOrWhiteSpace(normalizedSize)
                ? throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory)
                : normalizedSize,

            _ => throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory)
        };
    }

    private static string NormalizeShoeSize(string size)
    {
        if (!int.TryParse(size, out var numericSize))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory);
        }

        if (numericSize <= 0 || numericSize > 60)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory);
        }

        return numericSize.ToString();
    }

    private static string NormalizeClothingSize(string size)
    {
        var normalizedSize = size.ToUpperInvariant();

        if (!ProductVariantSizeConsts.IsValidClothingSize(normalizedSize))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantInvalidSizeForCategory);
        }

        return normalizedSize;
    }

    private static string NormalizeText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string NormalizeKey(string? value)
    {
        return NormalizeText(value).ToUpperInvariant();
    }

    private sealed record ProductVariantProductInfo(
        Guid ProductId,
        string ProductName,
        CategorySizeType SizeType
    );

    private sealed record NormalizedVariantItem(
        string Color,
        string Size,
        string NormalizedColor,
        string NormalizedSize,
        int StockQuantity,
        bool IsActive
    )
    {
        public string Key => $"{NormalizedColor}|{NormalizedSize}";
    }
}