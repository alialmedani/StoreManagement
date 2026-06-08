using System;
using System.Threading.Tasks;
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
            note
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

        var oldQuantity = variant.StockQuantity;
        var newQuantity = oldQuantity + quantityChange;

        var allowNegativeStock = await IsSettingEnabledAsync(
            StoreManagementSettings.AllowNegativeStock,
            defaultValue: false
        );

        if (!allowNegativeStock && newQuantity < 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryStockCannotBeNegative);
        }

        variant.SetStockQuantity(newQuantity);

        var movement = new StockMovement(
            GuidGenerator.Create(),
            variant.Id,
            movementType,
            quantityChange,
            oldQuantity,
            newQuantity,
            sourceType,
            referenceId,
            note
        );

        await _productVariantRepository.UpdateAsync(variant, autoSave: true);
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