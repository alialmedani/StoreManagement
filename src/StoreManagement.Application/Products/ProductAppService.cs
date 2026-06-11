using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StoreManagement.Categories;
using StoreManagement.Common;
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

public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;
    private readonly ISettingProvider _settingProvider;

    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductVariant, Guid> productVariantRepository,
        IRepository<Category, Guid> categoryRepository,
        IDataFilter<ISoftDelete> softDeleteFilter,
        ISettingProvider settingProvider)
    {
        _productRepository = productRepository;
        _productVariantRepository = productVariantRepository;
        _categoryRepository = categoryRepository;
        _softDeleteFilter = softDeleteFilter;
        _settingProvider = settingProvider;
    }

    public async Task<PagedResultDto<ProductDto>> GetListAsync(ProductPagedAndSortedResultRequestDto input)
    {
        var lowStockThreshold = await GetLowStockThresholdAsync();

        var query = await _productRepository.GetQueryableAsync();

        query = ApplyFilter(query, input.Filter);
        query = ApplyAvailabilityStatusFilter(query, input.AvailabilityStatus, lowStockThreshold);
        query = ApplySorting(query, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(MapToProductDtoExpression())
        );

        ApplyAvailabilityStatus(items, lowStockThreshold);

        return new PagedResultDto<ProductDto>(totalCount, items);
    }

    public async Task<PagedResultDto<ProductDto>> GetDeletedListAsync(ProductPagedAndSortedResultRequestDto input)
    {
        using (_softDeleteFilter.Disable())
        {
            var lowStockThreshold = await GetLowStockThresholdAsync();

            var query = await _productRepository.GetQueryableAsync();

            query = query.Where(product => product.IsDeleted);

            query = ApplyFilter(query, input.Filter);
            query = ApplyAvailabilityStatusFilter(query, input.AvailabilityStatus, lowStockThreshold);
            query = ApplySorting(query, input.Sorting);

            var totalCount = await AsyncExecuter.CountAsync(query);

            var items = await AsyncExecuter.ToListAsync(
                query
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
                    .Select(MapToProductDtoExpression())
            );

            ApplyAvailabilityStatus(items, lowStockThreshold);

            return new PagedResultDto<ProductDto>(totalCount, items);
        }
    }

    public async Task<ProductDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var lowStockThreshold = await GetLowStockThresholdAsync();

        var productQuery = await _productRepository.GetQueryableAsync();

        var productStocks = await AsyncExecuter.ToListAsync(
            productQuery
                .Select(product => new
                {
                    product.IsActive,
                    TotalStockQuantity = product.Variants
                        .Where(variant => !variant.IsDeleted)
                        .Sum(variant => variant.StockQuantity)
                })
        );

        var variantQuery = await _productVariantRepository.GetQueryableAsync();

        var variantStocks = await AsyncExecuter.ToListAsync(
            variantQuery
                .Select(variant => new
                {
                    variant.IsActive,
                    variant.StockQuantity
                })
        );

        var productStatuses = productStocks
            .Select(product => ProductAvailabilityCalculator.CalculateStatus(
                product.TotalStockQuantity,
                lowStockThreshold
            ))
            .ToList();

        var variantStatuses = variantStocks
            .Select(variant => ProductAvailabilityCalculator.CalculateStatus(
                variant.StockQuantity,
                lowStockThreshold
            ))
            .ToList();

        return new ProductDashboardSummaryDto
        {
            TotalProducts = productStocks.Count,
            ActiveProducts = productStocks.Count(product => product.IsActive),
            InactiveProducts = productStocks.Count(product => !product.IsActive),

            TotalVariants = variantStocks.Count,
            ActiveVariants = variantStocks.Count(variant => variant.IsActive),
            InactiveVariants = variantStocks.Count(variant => !variant.IsActive),

            TotalStockQuantity = variantStocks.Sum(variant => variant.StockQuantity),
            LowStockThreshold = lowStockThreshold,

            OutOfStockProducts = productStatuses.Count(status =>
                status == ProductAvailabilityStatus.OutOfStock),
            LowStockProducts = productStatuses.Count(status =>
                status == ProductAvailabilityStatus.LowStock),
            InStockProducts = productStatuses.Count(status =>
                status == ProductAvailabilityStatus.InStock),

            OutOfStockVariants = variantStatuses.Count(status =>
                status == ProductAvailabilityStatus.OutOfStock),
            LowStockVariants = variantStatuses.Count(status =>
                status == ProductAvailabilityStatus.LowStock),
            InStockVariants = variantStatuses.Count(status =>
                status == ProductAvailabilityStatus.InStock)
        };
    }

    public async Task<ProductDetailsDto> GetAsync(Guid id)
    {
        var query = await _productRepository.GetQueryableAsync();

        var product = await AsyncExecuter.FirstOrDefaultAsync(
            query
                .Where(product => product.Id == id)
                .Select(product => new ProductDetailsDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    TotalStockQuantity = product.Variants
                        .Where(variant => !variant.IsDeleted)
                        .Sum(variant => variant.StockQuantity),
                    IsActive = product.IsActive,
                    Category = new EntityLookupDto
                    {
                        Id = product.CategoryId,
                        Name = product.Category.Name
                    },
                    TargetAudience = new LookupDto
                    {
                        Id = (int)product.TargetAudience,
                        Name = product.TargetAudience.ToString()
                    },
                    Variants = product.Variants
                        .Where(variant => !variant.IsDeleted)
                        .Select(variant => new ProductVariantSummaryDto
                        {
                            Id = variant.Id,
                            Color = variant.Color,
                            Size = variant.Size,
                            StockQuantity = variant.StockQuantity,
                            IsActive = variant.IsActive
                        })
                        .ToList(),
                    CreationTime = product.CreationTime,
                    CreatorId = product.CreatorId,
                    LastModificationTime = product.LastModificationTime,
                    LastModifierId = product.LastModifierId,
                    IsDeleted = product.IsDeleted,
                    DeleterId = product.DeleterId,
                    DeletionTime = product.DeletionTime
                })
        );

        if (product == null)
        {
            throw new EntityNotFoundException(typeof(Product), id);
        }

        var lowStockThreshold = await GetLowStockThresholdAsync();

        product.AvailabilityStatus = ProductAvailabilityCalculator.CreateStatus(
            product.TotalStockQuantity,
            lowStockThreshold
        );

        product.Variants = product.Variants
            .OrderBy(variant => variant.Color)
            .ThenBy(variant => ProductVariantSizeConsts.GetSortOrder(variant.Size))
            .ThenBy(variant => variant.Size)
            .ToList();

        return product;
    }

    [Authorize(StoreManagementPermissions.Products.Create)]
    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        await EnsureCategoryExistsAsync(input.CategoryId);

        await EnsureProductNameIsUniqueAsync(
            input.CategoryId,
            NormalizeName(input.Name)
        );

        var product = new Product(
            GuidGenerator.Create(),
            input.Name,
            input.Description,
            input.Price,
            input.CategoryId,
            input.TargetAudience,
            input.IsActive
        );

        await _productRepository.InsertAsync(product, autoSave: true);

        return await GetDtoAsync(product.Id);
    }

    [Authorize(StoreManagementPermissions.Products.Edit)]
    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input)
    {
        var product = await _productRepository.GetAsync(id);

        await EnsureCategoryExistsAsync(input.CategoryId);

        await EnsureProductNameIsUniqueAsync(
            input.CategoryId,
            NormalizeName(input.Name),
            id
        );

        if (product.CategoryId != input.CategoryId)
        {
            using (_softDeleteFilter.Disable())
            {
                var hasVariants = await _productVariantRepository.AnyAsync(
                    variant => variant.ProductId == product.Id
                );

                if (hasVariants)
                {
                    throw new BusinessException(StoreManagementDomainErrorCodes.ProductCategoryCannotBeChanged);
                }
            }

            product.ChangeCategory(input.CategoryId);
        }

        product.Rename(input.Name);
        product.SetDescription(input.Description);
        product.SetPrice(input.Price);
        product.ChangeTargetAudience(input.TargetAudience);
        product.SetActive(input.IsActive);

        await _productRepository.UpdateAsync(product, autoSave: true);

        return await GetDtoAsync(product.Id);
    }

    [Authorize(StoreManagementPermissions.Products.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var product = await _productRepository.GetAsync(id);

        using (_softDeleteFilter.Disable())
        {
            var hasVariants = await _productVariantRepository.AnyAsync(
                variant => variant.ProductId == product.Id
            );

            if (hasVariants)
            {
                throw new BusinessException(StoreManagementDomainErrorCodes.ProductHasVariants);
            }
        }

        await _productRepository.DeleteAsync(product, autoSave: true);
    }

    [Authorize(StoreManagementPermissions.Products.Restore)]
    public async Task RestoreAsync(Guid id)
    {
        using (_softDeleteFilter.Disable())
        {
            var product = await _productRepository.FindAsync(id);

            if (product == null || !product.IsDeleted)
            {
                throw new EntityNotFoundException(typeof(Product), id);
            }

            var categoryExists = await _categoryRepository.AnyAsync(category =>
                category.Id == product.CategoryId &&
                !category.IsDeleted);

            if (!categoryExists)
            {
                throw new BusinessException(StoreManagementDomainErrorCodes.ProductCategoryNotFound);
            }

            await EnsureProductNameIsUniqueAsync(
                product.CategoryId,
                product.NormalizedName,
                product.Id
            );

            product.Restore();

            await _productRepository.UpdateAsync(product, autoSave: true);
        }
    }

    private async Task<ProductDto> GetDtoAsync(Guid id)
    {
        var query = await _productRepository.GetQueryableAsync();

        var product = await AsyncExecuter.FirstOrDefaultAsync(
            query
                .Where(product => product.Id == id)
                .Select(MapToProductDtoExpression())
        );

        if (product == null)
        {
            throw new EntityNotFoundException(typeof(Product), id);
        }

        var lowStockThreshold = await GetLowStockThresholdAsync();

        product.AvailabilityStatus = ProductAvailabilityCalculator.CreateStatus(
            product.TotalStockQuantity,
            lowStockThreshold
        );

        return product;
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductCategoryNotFound);
        }

        var categoryExists = await _categoryRepository.AnyAsync(category =>
            category.Id == categoryId &&
            !category.IsDeleted);

        if (!categoryExists)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductCategoryNotFound);
        }
    }

    private async Task EnsureProductNameIsUniqueAsync(
        Guid categoryId,
        string normalizedName,
        Guid? excludedProductId = null)
    {
        var duplicateExists = await _productRepository.AnyAsync(product =>
            !product.IsDeleted &&
            product.CategoryId == categoryId &&
            product.NormalizedName == normalizedName &&
            (!excludedProductId.HasValue || product.Id != excludedProductId.Value));

        if (duplicateExists)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductNameAlreadyExists);
        }
    }

    private static IQueryable<Product> ApplyFilter(IQueryable<Product> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return query;
        }

        var normalizedFilter = filter.Trim();

        return query.Where(product =>
            product.Name.Contains(normalizedFilter) ||
            (product.Description != null && product.Description.Contains(normalizedFilter)) ||
            product.Category.Name.Contains(normalizedFilter));
    }

    private static IQueryable<Product> ApplyAvailabilityStatusFilter(
        IQueryable<Product> query,
        ProductAvailabilityStatus? availabilityStatus,
        int lowStockThreshold)
    {
        if (!availabilityStatus.HasValue)
        {
            return query;
        }

        return availabilityStatus.Value switch
        {
            ProductAvailabilityStatus.OutOfStock => query.Where(product =>
                product.Variants
                    .Where(variant => !variant.IsDeleted)
                    .Sum(variant => variant.StockQuantity) <= 0),

            ProductAvailabilityStatus.LowStock => query.Where(product =>
                product.Variants
                    .Where(variant => !variant.IsDeleted)
                    .Sum(variant => variant.StockQuantity) > 0 &&
                product.Variants
                    .Where(variant => !variant.IsDeleted)
                    .Sum(variant => variant.StockQuantity) <= lowStockThreshold),

            ProductAvailabilityStatus.InStock => query.Where(product =>
                product.Variants
                    .Where(variant => !variant.IsDeleted)
                    .Sum(variant => variant.StockQuantity) > lowStockThreshold),

            _ => query
        };
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            return query.OrderByDescending(product => product.CreationTime);
        }

        return sorting.Trim().ToLowerInvariant() switch
        {
            "name" or "name asc" => query.OrderBy(product => product.Name),
            "name desc" => query.OrderByDescending(product => product.Name),

            "price" or "price asc" => query.OrderBy(product => product.Price),
            "price desc" => query.OrderByDescending(product => product.Price),

            "creationtime" or "creationtime desc" => query.OrderByDescending(product => product.CreationTime),
            "creationtime asc" => query.OrderBy(product => product.CreationTime),

            "isactive" or "isactive asc" => query.OrderBy(product => product.IsActive),
            "isactive desc" => query.OrderByDescending(product => product.IsActive),

            _ => query.OrderByDescending(product => product.CreationTime)
        };
    }

    private static Expression<Func<Product, ProductDto>> MapToProductDtoExpression()
    {
        return product => new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            TotalStockQuantity = product.Variants
                .Where(variant => !variant.IsDeleted)
                .Sum(variant => variant.StockQuantity),
            IsActive = product.IsActive,
            Category = new EntityLookupDto
            {
                Id = product.CategoryId,
                Name = product.Category.Name
            },
            TargetAudience = new LookupDto
            {
                Id = (int)product.TargetAudience,
                Name = product.TargetAudience.ToString()
            },
            CreationTime = product.CreationTime,
            CreatorId = product.CreatorId,
            LastModificationTime = product.LastModificationTime,
            LastModifierId = product.LastModifierId,
            IsDeleted = product.IsDeleted,
            DeleterId = product.DeleterId,
            DeletionTime = product.DeletionTime
        };
    }

    private async Task<int> GetLowStockThresholdAsync()
    {
        var value = await _settingProvider.GetOrNullAsync(StoreManagementSettings.LowStockThreshold);

        return ProductAvailabilityCalculator.NormalizeLowStockThreshold(value);
    }

    private static void ApplyAvailabilityStatus(
        List<ProductDto> items,
        int lowStockThreshold)
    {
        foreach (var item in items)
        {
            item.AvailabilityStatus = ProductAvailabilityCalculator.CreateStatus(
                item.TotalStockQuantity,
                lowStockThreshold
            );
        }
    }

    private static string NormalizeName(string name)
    {
        return name.Trim().ToUpperInvariant();
    }
}