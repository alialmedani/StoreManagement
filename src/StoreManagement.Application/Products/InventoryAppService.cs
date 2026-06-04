using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace StoreManagement.Inventory;

public class InventoryAppService : ApplicationService, IInventoryAppService
{
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;
    private readonly InventoryManager _inventoryManager;

    public InventoryAppService(
        IRepository<StockMovement, Guid> stockMovementRepository,
        InventoryManager inventoryManager)
    {
        _stockMovementRepository = stockMovementRepository;
        _inventoryManager = inventoryManager;
    }

    public async Task<PagedResultDto<StockMovementDto>> GetListAsync(StockMovementPagedRequestDto input)
    {
        var query = await _stockMovementRepository.GetQueryableAsync();

        if (input.ProductVariantId.HasValue)
        {
            query = query.Where(movement => movement.ProductVariantId == input.ProductVariantId.Value);
        }

        if (input.MovementType.HasValue)
        {
            query = query.Where(movement => movement.MovementType == input.MovementType.Value);
        }

        if (input.SourceType.HasValue)
        {
            query = query.Where(movement => movement.SourceType == input.SourceType.Value);
        }

        if (input.ReferenceId.HasValue)
        {
            query = query.Where(movement => movement.ReferenceId == input.ReferenceId.Value);
        }

        query = ApplyFilter(query, input.Filter);
        query = ApplySorting(query, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(MapToDtoExpression())
        );

        return new PagedResultDto<StockMovementDto>(totalCount, items);
    }

    public async Task<StockMovementDto> GetAsync(Guid id)
    {
        var query = await _stockMovementRepository.GetQueryableAsync();

        var movement = await AsyncExecuter.FirstOrDefaultAsync(
            query
                .Where(movement => movement.Id == id)
                .Select(MapToDtoExpression())
        );

        if (movement == null)
        {
            throw new EntityNotFoundException(typeof(StockMovement), id);
        }

        return movement;
    }

    public async Task<StockMovementDto> AdjustStockAsync(AdjustStockDto input)
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

    private static IQueryable<StockMovement> ApplyFilter(IQueryable<StockMovement> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return query;
        }

        var normalizedFilter = filter.Trim();

        return query.Where(movement =>
            movement.ProductVariant.Product.Name.Contains(normalizedFilter) ||
            movement.ProductVariant.Color.Contains(normalizedFilter) ||
            movement.ProductVariant.Size.Contains(normalizedFilter) ||
            (movement.Note != null && movement.Note.Contains(normalizedFilter)));
    }

    private static IQueryable<StockMovement> ApplySorting(IQueryable<StockMovement> query, string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            return query.OrderByDescending(movement => movement.CreationTime);
        }

        return sorting.Trim().ToLowerInvariant() switch
        {
            "movementtype" or "movementtype asc" => query.OrderBy(movement => movement.MovementType),
            "movementtype desc" => query.OrderByDescending(movement => movement.MovementType),

            "sourcetype" or "sourcetype asc" => query.OrderBy(movement => movement.SourceType),
            "sourcetype desc" => query.OrderByDescending(movement => movement.SourceType),

            "quantitychange" or "quantitychange asc" => query.OrderBy(movement => movement.QuantityChange),
            "quantitychange desc" => query.OrderByDescending(movement => movement.QuantityChange),

            "oldquantity" or "oldquantity asc" => query.OrderBy(movement => movement.OldQuantity),
            "oldquantity desc" => query.OrderByDescending(movement => movement.OldQuantity),

            "newquantity" or "newquantity asc" => query.OrderBy(movement => movement.NewQuantity),
            "newquantity desc" => query.OrderByDescending(movement => movement.NewQuantity),

            "creationtime" or "creationtime desc" => query.OrderByDescending(movement => movement.CreationTime),
            "creationtime asc" => query.OrderBy(movement => movement.CreationTime),

            _ => query.OrderByDescending(movement => movement.CreationTime)
        };
    }

    private static Expression<Func<StockMovement, StockMovementDto>> MapToDtoExpression()
    {
        return movement => new StockMovementDto
        {
            Id = movement.Id,
            ProductVariantId = movement.ProductVariantId,
            ProductName = movement.ProductVariant.Product.Name,
            Color = movement.ProductVariant.Color,
            Size = movement.ProductVariant.Size,
            MovementType = new LookupDto
            {
                Id = (int)movement.MovementType,
                Name = movement.MovementType.ToString()
            },
            QuantityChange = movement.QuantityChange,
            OldQuantity = movement.OldQuantity,
            NewQuantity = movement.NewQuantity,
            SourceType = new LookupDto
            {
                Id = (int)movement.SourceType,
                Name = movement.SourceType.ToString()
            },
            ReferenceId = movement.ReferenceId,
            Note = movement.Note,
            CreationTime = movement.CreationTime,
            CreatorId = movement.CreatorId,
            LastModificationTime = movement.LastModificationTime,
            LastModifierId = movement.LastModifierId,
            IsDeleted = movement.IsDeleted,
            DeleterId = movement.DeleterId,
            DeletionTime = movement.DeletionTime
        };
    }
}