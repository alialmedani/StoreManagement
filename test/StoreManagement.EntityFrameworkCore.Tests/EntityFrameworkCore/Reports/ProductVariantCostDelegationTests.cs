using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using StoreManagement.Categories;
using StoreManagement.Products;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace StoreManagement.EntityFrameworkCore.Reports;

/// <summary>
/// Confirms that both the create and update paths of <see cref="ProductVariantAppService"/>
/// route cost changes through <see cref="ProductVariantCostManager"/> (one history row per real
/// change), and that the bulk path never sets cost at all.
/// </summary>
[Collection(StoreManagementTestConsts.CollectionDefinitionName)]
public class ProductVariantCostDelegationTests : StoreManagementEntityFrameworkCoreTestBase
{
    private readonly IProductVariantAppService _variantAppService;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductVariantCostHistory, Guid> _costHistoryRepository;

    public ProductVariantCostDelegationTests()
    {
        _variantAppService = GetRequiredService<IProductVariantAppService>();
        _categoryRepository = GetRequiredService<IRepository<Category, Guid>>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        _costHistoryRepository = GetRequiredService<IRepository<ProductVariantCostHistory, Guid>>();
    }

    [Fact]
    public async Task Create_Path_Records_Initial_Cost_Through_The_Manager()
    {
        var productId = await CreateProductAsync();

        var variant = await _variantAppService.CreateAsync(new CreateProductVariantDto
        {
            ProductId = productId,
            Color = "Red",
            Size = "M",
            StockQuantity = 5,
            CostPrice = 10m
        });

        var history = await GetHistoryAsync(variant.Id);
        var entry = history.ShouldHaveSingleItem();
        entry.PreviousCostPrice.ShouldBeNull();
        entry.NewCostPrice.ShouldBe(10m);
    }

    [Fact]
    public async Task Update_Path_Records_A_Real_Cost_Change_And_Skips_A_NoOp()
    {
        var productId = await CreateProductAsync();

        var variant = await _variantAppService.CreateAsync(new CreateProductVariantDto
        {
            ProductId = productId,
            Color = "Blue",
            Size = "L",
            StockQuantity = 5,
            CostPrice = 10m
        });

        // Real change 10 -> 20.
        await _variantAppService.UpdateAsync(variant.Id, new UpdateProductVariantDto
        {
            Color = "Blue",
            Size = "L",
            IsActive = true,
            CostPrice = 20m
        });

        // No-op change 20 -> 20.
        await _variantAppService.UpdateAsync(variant.Id, new UpdateProductVariantDto
        {
            Color = "Blue",
            Size = "L",
            IsActive = true,
            CostPrice = 20m
        });

        var history = (await GetHistoryAsync(variant.Id))
            .OrderBy(h => h.CreationTime)
            .ToList();

        history.Count.ShouldBe(2); // create(=>10) + real update(10=>20); no-op recorded nothing
        history[0].NewCostPrice.ShouldBe(10m);
        history[1].PreviousCostPrice.ShouldBe(10m);
        history[1].NewCostPrice.ShouldBe(20m);
    }

    [Fact]
    public async Task Bulk_Create_Path_Does_Not_Set_Cost_Or_Write_History()
    {
        var productId = await CreateProductAsync();

        var variants = await _variantAppService.BulkCreateAsync(new CreateBulkProductVariantsDto
        {
            ProductId = productId,
            Variants = new List<CreateBulkProductVariantItemDto>
            {
                new() { Color = "Red", Size = "S", StockQuantity = 1 },
                new() { Color = "Red", Size = "XL", StockQuantity = 2 }
            }
        });

        foreach (var variant in variants)
        {
            variant.CostPrice.ShouldBeNull();
            (await GetHistoryAsync(variant.Id)).ShouldBeEmpty();
        }
    }

    private async Task<Guid> CreateProductAsync()
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var category = new Category(
                Guid.NewGuid(),
                "Cat-" + Guid.NewGuid().ToString("N"),
                null,
                CategorySizeType.Clothing);
            await _categoryRepository.InsertAsync(category, autoSave: true);

            var product = new Product(
                Guid.NewGuid(),
                "Prod-" + Guid.NewGuid().ToString("N"),
                null,
                100m,
                category.Id);
            await _productRepository.InsertAsync(product, autoSave: true);

            return product.Id;
        });
    }

    private async Task<List<ProductVariantCostHistory>> GetHistoryAsync(Guid variantId)
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var all = await _costHistoryRepository.GetListAsync();
            return all.Where(h => h.ProductVariantId == variantId).ToList();
        });
    }
}
