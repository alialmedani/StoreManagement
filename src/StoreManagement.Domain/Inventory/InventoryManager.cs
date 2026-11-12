using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Products;
using StoreManagement.Settings;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Settings;

namespace StoreManagement.Inventory;

public class InventoryManager : DomainService
{
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;
    private readonly ISettingProvider _settingProvider;

    public InventoryManager(
        IRepository<ProductVariant, Guid> productVariantRepository,
        IRepository<StockMovement, Guid> stockMovementRepository,
        ISettingProvider settingProvider)
    {
        _productVariantRepository = productVariantRepository;
        _stockMovementRepository = stockMovementRepository;
        _settingProvider = settingProvider;
    }

    public async Task<StockMovement> AdjustManuallyAsync(
        Guid productVariantId,
        StockMovementType movementType,
        int? quantity,
        int? newQuantity,
        string? note = null)
    {
        EnsureManualMovementTypeIsAllowed(movementType);

        var variant = await _productVariantRepository.FindAsync(productVariantId);

        if (variant == null)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryProductVariantNotFound);
        }

        var quantityChange = CalculateManualQuantityChange(
            movementType,
            quantity,
            newQuantity,
            variant.StockQuantity
        );

        return await ApplyStockChangeAsync(
            variant,
            movementType,
            quantityChange,
            StockMovementSourceType.Manual,
            null,
            note
        );
    }

    public async Task<StockMovement?> CreateOpeningStockAsync(
        ProductVariant variant,
        string? note = "Opening stock")
    {
        if (variant.StockQuantity <= 0)
        {
            return null;
        }

        var movement = new StockMovement(
            GuidGenerator.Create(),
            variant.Id,
            StockMovementType.OpeningStock,
            variant.StockQuantity,
            0,
            variant.StockQuantity,
            StockMovementSourceType.ProductVariant,
            variant.Id,
            note,
            allowNegativeStock: false
        );

        await _stockMovementRepository.InsertAsync(movement, autoSave: true);

        return movement;
    }

    public async Task<StockMovement> RecordSaleAsync(
        Guid orderId,
        Guid productVariantId,
        int quantity,
        string? note = null)
    {
        var variant = await GetVariantAsync(productVariantId);

        return await ApplyStockChangeAsync(
            variant,
            StockMovementType.Sale,
            -GetRequiredPositiveQuantity(quantity),
            StockMovementSourceType.Order,
            orderId,
            note
        );
    }

    public async Task<StockMovement> RecordOrderCancellationAsync(
        Guid orderId,
        Guid productVariantId,
        int quantity,
        string? note = null)
    {
        var variant = await GetVariantAsync(productVariantId);

        return await ApplyStockChangeAsync(
            variant,
            StockMovementType.OrderCancellation,
            GetRequiredPositiveQuantity(quantity),
            StockMovementSourceType.Order,
            orderId,
            note
        );
    }

    public async Task<bool> HasMovementsAsync(Guid productVariantId)
    {
        return await _stockMovementRepository.AnyAsync(movement =>
            movement.ProductVariantId == productVariantId);
    }

    private async Task<ProductVariant> GetVariantAsync(Guid productVariantId)
    {
        var variant = await _productVariantRepository.FindAsync(productVariantId);

        if (variant == null)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryProductVariantNotFound);
        }

        return variant;
    }

    /// <summary>
    /// Applies a stock change atomically at the database level, so two
    /// concurrent requests against the same variant (e.g. two staff
    /// confirming orders for the last unit at the same time) can never both
    /// succeed: the increment and the "would this go negative" guard happen
    /// in a single conditional UPDATE, not a read-then-write-later sequence.
    ///
    /// EF Core's <c>ExecuteUpdateAsync</c> was chosen over a RowVersion +
    /// retry-on-conflict approach because the caller (order confirmation)
    /// needs a clean reject, not a retry loop, and ExecuteUpdateAsync avoids
    /// loading the entity into the change tracker at all, so there is no
    /// separate write to reconcile afterwards. <c>_productVariantRepository</c>
    /// is deliberately not asked to update the in-memory <paramref name="variant"/>
    /// instance here - doing so would risk EF Core re-writing it a second time
    /// (non-atomically) on the request's next SaveChanges.
    /// </summary>
    private async Task<StockMovement> ApplyStockChangeAsync(
        ProductVariant variant,
        StockMovementType movementType,
        int quantityChange,
        StockMovementSourceType sourceType,
        Guid? referenceId,
        string? note = null)
    {
        if (quantityChange == 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryQuantityChangeCannotBeZero);
        }

        var allowNegativeStock = await IsSettingEnabledAsync(
            StoreManagementSettings.AllowNegativeStock,
            defaultValue: false
        );

        var variantQueryable = await _productVariantRepository.GetQueryableAsync();

        var affectedRows = await variantQueryable
            .Where(v =>
                v.Id == variant.Id &&
                (allowNegativeStock || v.StockQuantity + quantityChange >= 0))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(v => v.StockQuantity, v => v.StockQuantity + quantityChange));

        if (affectedRows == 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryStockCannotBeNegative);
        }

        // Safe to read back here: this request's transaction holds the row
        // lock it just took via the UPDATE above until it commits, so no
        // other transaction's write can be interleaved before this SELECT.
        var newQuantity = await variantQueryable
            .Where(v => v.Id == variant.Id)
            .Select(v => v.StockQuantity)
            .FirstAsync();

        var oldQuantity = newQuantity - quantityChange;

        var movement = new StockMovement(
            GuidGenerator.Create(),
            variant.Id,
            movementType,
            quantityChange,
            oldQuantity,
            newQuantity,
            sourceType,
            referenceId,
            note,
            allowNegativeStock: true // old/new are already guaranteed consistent and non-negative by the atomic update above
        );

        await _stockMovementRepository.InsertAsync(movement, autoSave: true);

        return movement;
    }

    private static void EnsureManualMovementTypeIsAllowed(StockMovementType movementType)
    {
        if (movementType is not (
                StockMovementType.Increase or
                StockMovementType.Decrease or
                StockMovementType.Adjustment))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryManualMovementTypeNotAllowed);
        }
    }

    private static int CalculateManualQuantityChange(
        StockMovementType movementType,
        int? quantity,
        int? newQuantity,
        int oldQuantity)
    {
        return movementType switch
        {
            StockMovementType.Increase => GetRequiredPositiveQuantity(quantity),

            StockMovementType.Decrease => -GetRequiredPositiveQuantity(quantity),

            StockMovementType.Adjustment => CalculateAdjustmentChange(newQuantity, oldQuantity),

            _ => throw new BusinessException(StoreManagementDomainErrorCodes.InventoryManualMovementTypeNotAllowed)
        };
    }

    private static int GetRequiredPositiveQuantity(int? quantity)
    {
        if (!quantity.HasValue || quantity.Value <= 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryQuantityChangeCannotBeZero);
        }

        return quantity.Value;
    }

    private static int GetRequiredPositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryQuantityChangeCannotBeZero);
        }

        return quantity;
    }

    private static int CalculateAdjustmentChange(int? newQuantity, int oldQuantity)
    {
        if (!newQuantity.HasValue)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryQuantityChangeCannotBeZero);
        }

        return newQuantity.Value - oldQuantity;
    }

    private async Task<bool> IsSettingEnabledAsync(string settingName, bool defaultValue)
    {
        var value = await _settingProvider.GetOrNullAsync(settingName);

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var result)
            ? result
            : defaultValue;
    }
}