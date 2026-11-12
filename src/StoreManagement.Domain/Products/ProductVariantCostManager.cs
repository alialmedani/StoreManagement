using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace StoreManagement.Products;

/// <summary>
/// Owns every change to a <see cref="ProductVariant"/>'s <see cref="ProductVariant.CostPrice"/>:
/// it validates the new value through the entity's guard, persists the variant and writes a
/// <see cref="ProductVariantCostHistory"/> audit row whenever the value actually changes.
/// A no-op change (new value equals the current value) records nothing.
/// </summary>
public class ProductVariantCostManager : DomainService
{
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly IRepository<ProductVariantCostHistory, Guid> _costHistoryRepository;

    public ProductVariantCostManager(
        IRepository<ProductVariant, Guid> productVariantRepository,
        IRepository<ProductVariantCostHistory, Guid> costHistoryRepository)
    {
        _productVariantRepository = productVariantRepository;
        _costHistoryRepository = costHistoryRepository;
    }

    /// <summary>
    /// Validates and applies <paramref name="newCostPrice"/> to <paramref name="variant"/> and records a
    /// history row when the value changes. The cost update and the history insert run in the caller's
    /// ambient unit of work, so they commit together. Validation runs through
    /// <see cref="ProductVariant.SetCostPrice"/> before any history row is written, so a rejected value
    /// leaves no orphaned history record.
    /// </summary>
    public async Task ChangeCostPriceAsync(ProductVariant variant, decimal? newCostPrice)
    {
        var previousCostPrice = variant.CostPrice;

        if (previousCostPrice == newCostPrice)
        {
            return;
        }

        variant.SetCostPrice(newCostPrice);

        await _productVariantRepository.UpdateAsync(variant, autoSave: true);

        await _costHistoryRepository.InsertAsync(
            new ProductVariantCostHistory(
                GuidGenerator.Create(),
                variant.Id,
                previousCostPrice,
                newCostPrice),
            autoSave: true);
    }
}
