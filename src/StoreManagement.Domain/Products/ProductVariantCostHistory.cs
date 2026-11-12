using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Products;

public class ProductVariantCostHistory : CreationAuditedEntity<Guid>
{
    public Guid ProductVariantId { get; private set; }

    public decimal? PreviousCostPrice { get; private set; }

    public decimal? NewCostPrice { get; private set; }

    protected ProductVariantCostHistory()
    {
    }

    public ProductVariantCostHistory(
        Guid id,
        Guid productVariantId,
        decimal? previousCostPrice,
        decimal? newCostPrice)
        : base(id)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantProductNotFound);
        }

        ProductVariantId = productVariantId;
        PreviousCostPrice = previousCostPrice;
        NewCostPrice = newCostPrice;
    }
}
