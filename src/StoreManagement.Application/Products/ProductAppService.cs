using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using StoreManagement.Categories;
using StoreManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace StoreManagement.Products;

public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductVariant, Guid> productVariantRepository,
        IRepository<Category, Guid> categoryRepository,
        IDataFilter<ISoftDelete> softDeleteFilter)
    {
        _productRepository = productRepository;
        _productVariantRepository = productVariantRepository;
        _categoryRepository = categoryRepository;
        _softDeleteFilter = softDeleteFilter;
    }

    public async Task<PagedResultDto<ProductDto>> GetListAsync(StoreManagementPagedAndSortedResultRequestDto input)
    {
        var query = await _productRepository.GetQueryableAsync();

        query = ApplyFilter(query, input.Filter);
        query = ApplySorting(query, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(MapToProductDtoExpression())
        );

        return new PagedResultDto<ProductDto>(totalCount, items);
    }

    public async Task<PagedResultDto<ProductDto>> GetDeletedListAsync(StoreManagementPagedAndSortedResultRequestDto input)
    {
        using (_softDeleteFilter.Disable())
        {
            var query = await _productRepository.GetQueryableAsync();

            query = query.Where(product => product.IsDeleted);

            query = ApplyFilter(query, input.Filter);
            query = ApplySorting(query, input.Sorting);

            var totalCount = await AsyncExecuter.CountAsync(query);

            var items = await AsyncExecuter.ToListAsync(
                query
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
                    .Select(MapToProductDtoExpression())
            );

            return new PagedResultDto<ProductDto>(totalCount, items);
        }
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

        product.Variants = product.Variants
            .OrderBy(variant => variant.Color)
            .ThenBy(variant => ProductVariantSizeConsts.GetSortOrder(variant.Size))
            .ThenBy(variant => variant.Size)
            .ToList();

        return product;
    }

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

    private static string NormalizeName(string name)
    {
        return name.Trim().ToUpperInvariant();
    }
}