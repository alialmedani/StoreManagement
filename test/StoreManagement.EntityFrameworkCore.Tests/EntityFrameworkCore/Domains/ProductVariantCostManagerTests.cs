using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using StoreManagement.Categories;
using StoreManagement.Products;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace StoreManagement.EntityFrameworkCore.Domains;

[Collection(StoreManagementTestConsts.CollectionDefinitionName)]
public class ProductVariantCostManagerTests : StoreManagementEntityFrameworkCoreTestBase
{
    private readonly ProductVariantCostManager _costManager;
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<ProductVariantCostHistory, Guid> _costHistoryRepository;

    public ProductVariantCostManagerTests()
    {
        _costManager = GetRequiredService<ProductVariantCostManager>();
        _productVariantRepository = GetRequiredService<IRepository<ProductVariant, Guid>>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        _categoryRepository = GetRequiredService<IRepository<Category, Guid>>();
        _costHistoryRepository = GetRequiredService<IRepository<ProductVariantCostHistory, Guid>>();
    }

    [Fact]
    public async Task Should_Create_Exactly_One_History_Entry_When_Cost_Changes()
    {
        var variantId = await CreateVariantAsync(initialCost: null);

        await WithUnitOfWorkAsync(async () =>
        {
            var variant = await _productVariantRepository.GetAsync(variantId);
            await _costManager.ChangeCostPriceAsync(variant, 25m);
        });

        var history = await GetHistoryAsync(variantId);
        history.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Create_History_When_Cost_Is_Unchanged()
    {
        var variantId = await CreateVariantAsync(initialCost: 40m);

        await WithUnitOfWorkAsync(async () =>
        {
            var variant = await _productVariantRepository.GetAsync(variantId);
            await _costManager.ChangeCostPriceAsync(variant, 40m);
        });

        var history = await GetHistoryAsync(variantId);
        history.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Record_Correct_Previous_And_New_Cost()
    {
        var variantId = await CreateVariantAsync(initialCost: 10m);

        await WithUnitOfWorkAsync(async () =>
        {
            var variant = await _productVariantRepository.GetAsync(variantId);
            await _costManager.ChangeCostPriceAsync(variant, 30m);
        });

        var history = await GetHistoryAsync(variantId);
        var entry = history.ShouldHaveSingleItem();
        entry.PreviousCostPrice.ShouldBe(10m);
        entry.NewCostPrice.ShouldBe(30m);
    }

    [Fact]
    public async Task Should_Preserve_Creation_Audit_On_History_Entry()
    {
        var variantId = await CreateVariantAsync(initialCost: null);

        await WithUnitOfWorkAsync(async () =>
        {
            var variant = await _productVariantRepository.GetAsync(variantId);
            await _costManager.ChangeCostPriceAsync(variant, 15m);
        });

        var history = await GetHistoryAsync(variantId);
        var entry = history.ShouldHaveSingleItem();
        entry.CreationTime.ShouldNotBe(default);
    }

    [Fact]
    public async Task Should_Not_Write_History_When_Cost_Is_Rejected()
    {
        var variantId = await CreateVariantAsync(initialCost: 20m);

        await Should.ThrowAsync<BusinessException>(async () =>
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var variant = await _productVariantRepository.GetAsync(variantId);
                await _costManager.ChangeCostPriceAsync(variant, -5m);
            });
        });

        var history = await GetHistoryAsync(variantId);
        history.ShouldBeEmpty();

        var persisted = await WithUnitOfWorkAsync(() => _productVariantRepository.GetAsync(variantId));
        persisted.CostPrice.ShouldBe(20m);
    }

    private async Task<Guid> CreateVariantAsync(decimal? initialCost)
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

            var variant = new ProductVariant(
                Guid.NewGuid(),
                product.Id,
                "Red",
                "M",
                5,
                true,
                initialCost);
            await _productVariantRepository.InsertAsync(variant, autoSave: true);

            return variant.Id;
        });
    }

    private async Task<System.Collections.Generic.List<ProductVariantCostHistory>> GetHistoryAsync(Guid variantId)
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var all = await _costHistoryRepository.GetListAsync();
            return all.Where(h => h.ProductVariantId == variantId).ToList();
        });
    }
}
