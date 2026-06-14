using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StoreManagement.Common;
using StoreManagement.Permissions;
using StoreManagement.Products;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace StoreManagement.Inventory;

public class InventoryAppService :
    ApplicationService,
    IInventoryAppService
{
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly InventoryManager _inventoryManager;

    public InventoryAppService(
        IRepository<StockMovement, Guid> stockMovementRepository,
        IRepository<ProductVariant, Guid> productVariantRepository,
        InventoryManager inventoryManager)
    {
        _stockMovementRepository = stockMovementRepository;
        _productVariantRepository = productVariantRepository;
        _inventoryManager = inventoryManager;
    }

    public async Task<PagedResultDto<StockMovementDto>> GetListAsync(
        StockMovementPagedRequestDto input)
    {
        var query = await _stockMovementRepository.GetQueryableAsync();

        query = ApplyStructuredFilters(query, input);
        query = ApplyTextFilter(query, input.Filter);
        query = ApplySorting(query, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(MapToDtoExpression())
        );

        return new PagedResultDto<StockMovementDto>(
            totalCount,
            items
        );
    }

    public async Task<StockMovementDto> GetAsync(Guid id)
    {
        var query = await _stockMovementRepository.GetQueryableAsync();

        var movement = await AsyncExecuter.FirstOrDefaultAsync(
            query
                .Where(item => item.Id == id)
                .Select(MapToDtoExpression())
        );

        if (movement == null)
        {
            throw new EntityNotFoundException(
                typeof(StockMovement),
                id
            );
        }

        return movement;
    }

    public async Task<InventoryVariantHistorySummaryDto>
        GetVariantHistorySummaryAsync(Guid productVariantId)
    {
        var variantQuery =
            await _productVariantRepository.GetQueryableAsync();

        var summary = await AsyncExecuter.FirstOrDefaultAsync(
            variantQuery
                .Where(variant => variant.Id == productVariantId)
                .Select(variant => new InventoryVariantHistorySummaryDto
                {
                    CategoryId = variant.Product.CategoryId,
                    CategoryName = variant.Product.Category.Name,

                    ProductId = variant.ProductId,
                    ProductName = variant.Product.Name,

                    ProductVariantId = variant.Id,
                    Color = variant.Color,
                    Size = variant.Size,

                    CurrentStockQuantity = variant.StockQuantity
                })
        );

        if (summary == null)
        {
            throw new EntityNotFoundException(
                typeof(ProductVariant),
                productVariantId
            );
        }

        var movementQuery =
            await _stockMovementRepository.GetQueryableAsync();

        var statistics = await AsyncExecuter.FirstOrDefaultAsync(
            movementQuery
                .Where(movement =>
                    movement.ProductVariantId == productVariantId)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    TotalMovements = group.Count(),

                    TotalInboundQuantity = group.Sum(movement =>
                        movement.QuantityChange > 0
                            ? movement.QuantityChange
                            : 0),

                    TotalOutboundQuantity = group.Sum(movement =>
                        movement.QuantityChange < 0
                            ? -movement.QuantityChange
                            : 0),

                    LastMovementTime = group.Max(movement =>
                        (DateTime?)movement.CreationTime)
                })
        );

        if (statistics != null)
        {
            summary.TotalMovements =
                statistics.TotalMovements;

            summary.TotalInboundQuantity =
                statistics.TotalInboundQuantity;

            summary.TotalOutboundQuantity =
                statistics.TotalOutboundQuantity;

            summary.LastMovementTime =
                statistics.LastMovementTime;
        }

        return summary;
    }

    [Authorize(StoreManagementPermissions.Inventory.AdjustStock)]
    public async Task<StockMovementDto> AdjustStockAsync(
        AdjustStockDto input)
    {
        var movement = await _inventoryManager.AdjustManuallyAsync(
            input.ProductVariantId,
            input.MovementType,
            input.Quantity,
            input.NewQuantity,
            input.Note
        );

        return await GetAsync(movement.Id);
    }

    private static IQueryable<StockMovement> ApplyStructuredFilters(
        IQueryable<StockMovement> query,
        StockMovementPagedRequestDto input)
    {
        if (input.CategoryId.HasValue &&
            input.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(movement =>
                movement.ProductVariant.Product.CategoryId ==
                input.CategoryId.Value);
        }

        if (input.ProductId.HasValue &&
            input.ProductId.Value != Guid.Empty)
        {
            query = query.Where(movement =>
                movement.ProductVariant.ProductId ==
                input.ProductId.Value);
        }

        if (input.ProductVariantId.HasValue &&
            input.ProductVariantId.Value != Guid.Empty)
        {
            query = query.Where(movement =>
                movement.ProductVariantId ==
                input.ProductVariantId.Value);
        }

        if (input.MovementType.HasValue)
        {
            query = query.Where(movement =>
                movement.MovementType ==
                input.MovementType.Value);
        }

        if (input.SourceType.HasValue)
        {
            query = query.Where(movement =>
                movement.SourceType ==
                input.SourceType.Value);
        }

        if (input.ReferenceId.HasValue &&
            input.ReferenceId.Value != Guid.Empty)
        {
            query = query.Where(movement =>
                movement.ReferenceId ==
                input.ReferenceId.Value);
        }

        if (input.FromDate.HasValue)
        {
            var fromDate = input.FromDate.Value.Date;

            query = query.Where(movement =>
                movement.CreationTime >= fromDate);
        }

        if (input.ToDate.HasValue)
        {
            var toDateExclusive =
                input.ToDate.Value.Date.AddDays(1);

            query = query.Where(movement =>
                movement.CreationTime < toDateExclusive);
        }

        return query;
    }

    private static IQueryable<StockMovement> ApplyTextFilter(
        IQueryable<StockMovement> query,
        string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return query;
        }

        var normalizedFilter = filter.Trim();

        return query.Where(movement =>
            movement.ProductVariant.Product.Name.Contains(normalizedFilter) ||
            movement.ProductVariant.Product.Category.Name.Contains(normalizedFilter) ||
            movement.ProductVariant.Color.Contains(normalizedFilter) ||
            movement.ProductVariant.Size.Contains(normalizedFilter) ||
            (
                movement.Note != null &&
                movement.Note.Contains(normalizedFilter)
            ));
    }

    private static IQueryable<StockMovement> ApplySorting(
        IQueryable<StockMovement> query,
        string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            return query.OrderByDescending(movement =>
                movement.CreationTime);
        }

        return sorting.Trim().ToLowerInvariant() switch
        {
            "categoryname" or "categoryname asc" =>
                query.OrderBy(movement =>
                    movement.ProductVariant.Product.Category.Name),

            "categoryname desc" =>
                query.OrderByDescending(movement =>
                    movement.ProductVariant.Product.Category.Name),

            "productname" or "productname asc" =>
                query.OrderBy(movement =>
                    movement.ProductVariant.Product.Name),

            "productname desc" =>
                query.OrderByDescending(movement =>
                    movement.ProductVariant.Product.Name),

            "color" or "color asc" =>
                query.OrderBy(movement =>
                    movement.ProductVariant.Color),

            "color desc" =>
                query.OrderByDescending(movement =>
                    movement.ProductVariant.Color),

            "size" or "size asc" =>
                query.OrderBy(movement =>
                    movement.ProductVariant.Size),

            "size desc" =>
                query.OrderByDescending(movement =>
                    movement.ProductVariant.Size),

            "movementtype" or "movementtype asc" =>
                query.OrderBy(movement =>
                    movement.MovementType),

            "movementtype desc" =>
                query.OrderByDescending(movement =>
                    movement.MovementType),

            "sourcetype" or "sourcetype asc" =>
                query.OrderBy(movement =>
                    movement.SourceType),

            "sourcetype desc" =>
                query.OrderByDescending(movement =>
                    movement.SourceType),

            "quantitychange" or "quantitychange asc" =>
                query.OrderBy(movement =>
                    movement.QuantityChange),

            "quantitychange desc" =>
                query.OrderByDescending(movement =>
                    movement.QuantityChange),

            "oldquantity" or "oldquantity asc" =>
                query.OrderBy(movement =>
                    movement.OldQuantity),

            "oldquantity desc" =>
                query.OrderByDescending(movement =>
                    movement.OldQuantity),

            "newquantity" or "newquantity asc" =>
                query.OrderBy(movement =>
                    movement.NewQuantity),

            "newquantity desc" =>
                query.OrderByDescending(movement =>
                    movement.NewQuantity),

            "creationtime" or "creationtime desc" =>
                query.OrderByDescending(movement =>
                    movement.CreationTime),

            "creationtime asc" =>
                query.OrderBy(movement =>
                    movement.CreationTime),

            _ => query.OrderByDescending(movement =>
                movement.CreationTime)
        };
    }

    private static Expression<Func<StockMovement, StockMovementDto>>
        MapToDtoExpression()
    {
        return movement => new StockMovementDto
        {
            Id = movement.Id,

            CategoryId =
                movement.ProductVariant.Product.CategoryId,

            CategoryName =
                movement.ProductVariant.Product.Category.Name,

            ProductId =
                movement.ProductVariant.ProductId,

            ProductName =
                movement.ProductVariant.Product.Name,

            ProductVariantId =
                movement.ProductVariantId,

            Color =
                movement.ProductVariant.Color,

            Size =
                movement.ProductVariant.Size,

            CurrentStockQuantity =
                movement.ProductVariant.StockQuantity,

            MovementType = new LookupDto
            {
                Id = (int)movement.MovementType,
                Name = movement.MovementType.ToString()
            },

            QuantityChange =
                movement.QuantityChange,

            OldQuantity =
                movement.OldQuantity,

            NewQuantity =
                movement.NewQuantity,

            SourceType = new LookupDto
            {
                Id = (int)movement.SourceType,
                Name = movement.SourceType.ToString()
            },

            ReferenceId =
                movement.ReferenceId,

            Note =
                movement.Note,

            CreationTime =
                movement.CreationTime,

            CreatorId =
                movement.CreatorId,

            LastModificationTime =
                movement.LastModificationTime,

            LastModifierId =
                movement.LastModifierId,

            IsDeleted =
                movement.IsDeleted,

            DeleterId =
                movement.DeleterId,

            DeletionTime =
                movement.DeletionTime
        };
    }
}