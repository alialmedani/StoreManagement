using System;
using StoreManagement.Products;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Inventory;

public class StockMovement : FullAuditedEntity<Guid>
{
    public Guid ProductVariantId { get; private set; }

    public ProductVariant ProductVariant { get; private set; } = null!;

    public StockMovementType MovementType { get; private set; }

    public int QuantityChange { get; private set; }

    public int OldQuantity { get; private set; }

    public int NewQuantity { get; private set; }

    public StockMovementSourceType SourceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Note { get; private set; }

    protected StockMovement()
    {
    }

    public StockMovement(
        Guid id,
        Guid productVariantId,
        StockMovementType movementType,
        int quantityChange,
        int oldQuantity,
        int newQuantity,
        StockMovementSourceType sourceType,
        Guid? referenceId = null,
        string? note = null)
        : base(id)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryProductVariantNotFound);
        }

        if (!Enum.IsDefined(typeof(StockMovementType), movementType))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryQuantityChangeCannotBeZero);
        }

        if (!Enum.IsDefined(typeof(StockMovementSourceType), sourceType))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryInvalidMovementSource);
        }

        if (sourceType != StockMovementSourceType.Manual && !referenceId.HasValue)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryInvalidMovementSource);
        }

        if (quantityChange == 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryQuantityChangeCannotBeZero);
        }

        if (oldQuantity < 0 || newQuantity < 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryStockCannotBeNegative);
        }

        if (oldQuantity + quantityChange != newQuantity)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryStockCannotBeNegative);
        }

        ProductVariantId = productVariantId;
        MovementType = movementType;
        QuantityChange = quantityChange;
        OldQuantity = oldQuantity;
        NewQuantity = newQuantity;
        SourceType = sourceType;
        ReferenceId = referenceId;
        SetNote(note);
    }

    public void SetNote(string? note)
    {
        var normalizedNote = note?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedNote))
        {
            Note = null;
            return;
        }

        if (normalizedNote.Length > InventoryConsts.MaxNoteLength)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.InventoryNoteTooLong);
        }

        Note = normalizedNote;
    }
}