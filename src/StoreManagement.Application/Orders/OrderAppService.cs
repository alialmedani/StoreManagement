using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StoreManagement.Common;
using StoreManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace StoreManagement.Orders;

public class OrderAppService :
ApplicationService,
IOrderAppService
{
private readonly IRepository<Order, Guid> _orderRepository;
private readonly OrderManager _orderManager;

public OrderAppService(
    IRepository<Order, Guid> orderRepository,
    OrderManager orderManager)
{
    _orderRepository = orderRepository;
    _orderManager = orderManager;
}

public async Task<PagedResultDto<OrderDto>>
    GetListAsync(OrderPagedRequestDto input)
{
    var query =
        await _orderRepository.GetQueryableAsync();

    if (input.Status.HasValue)
    {
        query = query.Where(order =>
            order.Status == input.Status.Value
        );
    }

    if (input.PaymentStatus.HasValue)
    {
        query = query.Where(order =>
            order.PaymentStatus ==
            input.PaymentStatus.Value
        );
    }

    if (input.FromDate.HasValue)
    {
        var fromDate = input.FromDate.Value.Date;

        query = query.Where(order =>
            order.CreationTime >= fromDate
        );
    }

    if (input.ToDate.HasValue)
    {
        var toDateExclusive =
            input.ToDate.Value.Date.AddDays(1);

        query = query.Where(order =>
            order.CreationTime < toDateExclusive
        );
    }

    query = ApplyFilter(query, input.Filter);
    query = ApplySorting(query, input.Sorting);

    var totalCount =
        await AsyncExecuter.CountAsync(query);

    var items =
        await AsyncExecuter.ToListAsync(
            query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(MapToDtoExpression())
        );

    return new PagedResultDto<OrderDto>(
        totalCount,
        items
    );
}

public async Task<OrderDetailsDto> GetAsync(Guid id)
{
    var query =
        await _orderRepository.GetQueryableAsync();

    var order =
        await AsyncExecuter.FirstOrDefaultAsync(
            query
                .Where(order => order.Id == id)
                .Select(
                    MapToDetailsDtoExpression()
                )
        );

    if (order == null)
    {
        throw new EntityNotFoundException(
            typeof(Order),
            id
        );
    }

    return order;
}

[Authorize(StoreManagementPermissions.Orders.Create)]
public async Task<OrderDetailsDto> CreateAsync(
    CreateOrderDto input)
{
    if (input.Items == null ||
        input.Items.Count == 0)
    {
        throw new BusinessException(
            StoreManagementDomainErrorCodes
                .OrderItemRequired
        );
    }

    var order =
        await _orderManager.CreateAsync(
            input.CustomerName,
            input.CustomerAddress,
            input.CustomerPhone,
            input.Note
        );

    foreach (var item in input.Items)
    {
        await _orderManager.AddItemAsync(
            order,
            item.ProductVariantId,
            item.Quantity
        );
    }

    await _orderRepository.InsertAsync(
        order,
        autoSave: true
    );

    return await GetAsync(order.Id);
}

[Authorize(StoreManagementPermissions.Orders.Edit)]
public async Task<OrderDetailsDto> UpdateAsync(
    Guid id,
    UpdateOrderDto input)
{
    var order =
        await GetOrderAggregateAsync(id);

    order.UpdateHeader(
        input.CustomerName,
        input.CustomerAddress,
        input.CustomerPhone,
        input.Note
    );

    await _orderRepository.UpdateAsync(
        order,
        autoSave: true
    );

    return await GetAsync(order.Id);
}

[Authorize(StoreManagementPermissions.Orders.Edit)]
public async Task<OrderDetailsDto> AddItemAsync(
    Guid id,
    AddOrderItemDto input)
{
    var order =
        await GetOrderAggregateAsync(id);

    await _orderManager.AddItemAsync(
        order,
        input.ProductVariantId,
        input.Quantity
    );

    await _orderRepository.UpdateAsync(
        order,
        autoSave: true
    );

    return await GetAsync(order.Id);
}

[Authorize(StoreManagementPermissions.Orders.Edit)]
public async Task<OrderDetailsDto> UpdateItemAsync(
    Guid id,
    Guid itemId,
    UpdateOrderItemDto input)
{
    var order =
        await GetOrderAggregateAsync(id);

    await _orderManager.UpdateItemAsync(
        order,
        itemId,
        input.Quantity
    );

    await _orderRepository.UpdateAsync(
        order,
        autoSave: true
    );

    return await GetAsync(order.Id);
}

[Authorize(StoreManagementPermissions.Orders.Edit)]
public async Task<OrderDetailsDto> RemoveItemAsync(
    Guid id,
    Guid itemId)
{
    var order =
        await GetOrderAggregateAsync(id);

    order.RemoveItem(itemId);

    await _orderRepository.UpdateAsync(
        order,
        autoSave: true
    );

    return await GetAsync(order.Id);
}

[Authorize(StoreManagementPermissions.Orders.Confirm)]
public async Task<OrderDetailsDto> ConfirmAsync(
    Guid id)
{
    var order =
        await GetOrderAggregateAsync(id);

    await _orderManager.ConfirmAsync(order);

    await _orderRepository.UpdateAsync(
        order,
        autoSave: true
    );

    return await GetAsync(order.Id);
}

[Authorize(
    StoreManagementPermissions.Orders.RecordPayment
)]
public async Task<OrderDetailsDto> RecordPaymentAsync(
    Guid id,
    RecordOrderPaymentDto input)
{
    var order =
        await GetOrderAggregateAsync(id);

    _orderManager.RecordPayment(
        order,
        input.Amount,
        input.PaymentMethod,
        input.PaymentDate,
        input.ReferenceNumber,
        input.Note
    );

    await _orderRepository.UpdateAsync(
        order,
        autoSave: true
    );

    return await GetAsync(order.Id);
}

[Authorize(StoreManagementPermissions.Orders.Cancel)]
public async Task<OrderDetailsDto> CancelAsync(
    Guid id,
    CancelOrderDto input)
{
    var order =
        await GetOrderAggregateAsync(id);

    await _orderManager.CancelAsync(
        order,
        input.CancellationReason
    );

    await _orderRepository.UpdateAsync(
        order,
        autoSave: true
    );

    return await GetAsync(order.Id);
}

[Authorize(StoreManagementPermissions.Orders.Delete)]
public async Task DeleteAsync(Guid id)
{
    var order =
        await GetOrderAggregateAsync(id);

    if (!order.IsDraft())
    {
        throw new BusinessException(
            StoreManagementDomainErrorCodes
                .OrderCannotBeDeleted
        );
    }

    await _orderRepository.DeleteAsync(
        order,
        autoSave: true
    );
}

private async Task<Order> GetOrderAggregateAsync(
    Guid id)
{
    var query =
        await _orderRepository.WithDetailsAsync(
            order => order.Items,
            order => order.Payments
        );

    var order =
        await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(order => order.Id == id)
        );

    if (order == null)
    {
        throw new EntityNotFoundException(
            typeof(Order),
            id
        );
    }

    return order;
}

private static IQueryable<Order> ApplyFilter(
    IQueryable<Order> query,
    string? filter)
{
    if (string.IsNullOrWhiteSpace(filter))
    {
        return query;
    }

    var normalizedFilter = filter.Trim();

    return query.Where(order =>
        order.OrderNumber.Contains(normalizedFilter) ||
        order.CustomerName.Contains(normalizedFilter) ||
        order.CustomerAddress.Contains(normalizedFilter) ||
        (
            order.CustomerPhone != null &&
            order.CustomerPhone.Contains(normalizedFilter)
        ) ||
        (
            order.Note != null &&
            order.Note.Contains(normalizedFilter)
        ) ||
        (
            order.CancellationReason != null &&
            order.CancellationReason.Contains(normalizedFilter)
        ));
}

private static IQueryable<Order> ApplySorting(
    IQueryable<Order> query,
    string? sorting)
{
    if (string.IsNullOrWhiteSpace(sorting))
    {
        return query.OrderByDescending(order =>
            order.CreationTime
        );
    }

    return sorting
        .Trim()
        .ToLowerInvariant() switch
    {
        "ordernumber" or "ordernumber asc" =>
            query.OrderBy(order =>
                order.OrderNumber),

        "ordernumber desc" =>
            query.OrderByDescending(order =>
                order.OrderNumber),

        "customername" or "customername asc" =>
            query.OrderBy(order =>
                order.CustomerName),

        "customername desc" =>
            query.OrderByDescending(order =>
                order.CustomerName),

        "status" or "status asc" =>
            query.OrderBy(order =>
                order.Status),

        "status desc" =>
            query.OrderByDescending(order =>
                order.Status),

        "paymentstatus" or "paymentstatus asc" =>
            query.OrderBy(order =>
                order.PaymentStatus),

        "paymentstatus desc" =>
            query.OrderByDescending(order =>
                order.PaymentStatus),

        "totalamount" or "totalamount asc" =>
            query.OrderBy(order =>
                order.TotalAmount),

        "totalamount desc" =>
            query.OrderByDescending(order =>
                order.TotalAmount),

        "paidamount" or "paidamount asc" =>
            query.OrderBy(order =>
                order.PaidAmount),

        "paidamount desc" =>
            query.OrderByDescending(order =>
                order.PaidAmount),

        "cancellationtime" or "cancellationtime asc" =>
            query.OrderBy(order =>
                order.CancellationTime),

        "cancellationtime desc" =>
            query.OrderByDescending(order =>
                order.CancellationTime),

        "creationtime" or "creationtime desc" =>
            query.OrderByDescending(order =>
                order.CreationTime),

        "creationtime asc" =>
            query.OrderBy(order =>
                order.CreationTime),

        _ => query.OrderByDescending(order =>
            order.CreationTime)
    };
}

private static Expression<Func<Order, OrderDto>>
    MapToDtoExpression()
{
    return order => new OrderDto
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        CustomerName = order.CustomerName,
        CustomerAddress = order.CustomerAddress,
        CustomerPhone = order.CustomerPhone,
        Note = order.Note,

        Status = new LookupDto
        {
            Id = (int)order.Status,
            Name = order.Status.ToString()
        },

        TotalAmount = order.TotalAmount,

        PaymentStatus = new LookupDto
        {
            Id = (int)order.PaymentStatus,
            Name = order.PaymentStatus.ToString()
        },

        PaidAmount = order.PaidAmount,

        RemainingAmount =
            order.Status == OrderStatus.Cancelled
                ? 0m
                : order.TotalAmount > order.PaidAmount
                    ? order.TotalAmount -
                      order.PaidAmount
                    : 0m,

        CancellationReason =
            order.CancellationReason,

        CancellationTime =
            order.CancellationTime,

        CreationTime = order.CreationTime,
        CreatorId = order.CreatorId,

        LastModificationTime =
            order.LastModificationTime,

        LastModifierId =
            order.LastModifierId,

        IsDeleted = order.IsDeleted,
        DeleterId = order.DeleterId,
        DeletionTime = order.DeletionTime
    };
}

private static Expression<Func<Order, OrderDetailsDto>>
    MapToDetailsDtoExpression()
{
    return order => new OrderDetailsDto
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        CustomerName = order.CustomerName,
        CustomerAddress = order.CustomerAddress,
        CustomerPhone = order.CustomerPhone,
        Note = order.Note,

        Status = new LookupDto
        {
            Id = (int)order.Status,
            Name = order.Status.ToString()
        },

        TotalAmount = order.TotalAmount,

        PaymentStatus = new LookupDto
        {
            Id = (int)order.PaymentStatus,
            Name = order.PaymentStatus.ToString()
        },

        PaidAmount = order.PaidAmount,

        RemainingAmount =
            order.Status == OrderStatus.Cancelled
                ? 0m
                : order.TotalAmount > order.PaidAmount
                    ? order.TotalAmount -
                      order.PaidAmount
                    : 0m,

        CancellationReason =
            order.CancellationReason,

        CancellationTime =
            order.CancellationTime,

        Items = order.Items
            .OrderBy(item => item.CreationTime)
            .Select(item => new OrderItemDto
            {
                Id = item.Id,
                OrderId = item.OrderId,

                ProductVariantId =
                    item.ProductVariantId,

                ProductName =
                    item.ProductName,

                Color = item.Color,
                Size = item.Size,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal,

                CreationTime =
                    item.CreationTime,

                CreatorId =
                    item.CreatorId,

                LastModificationTime =
                    item.LastModificationTime,

                LastModifierId =
                    item.LastModifierId,

                IsDeleted =
                    item.IsDeleted,

                DeleterId =
                    item.DeleterId,

                DeletionTime =
                    item.DeletionTime
            })
            .ToList(),

        Payments = order.Payments
            .OrderBy(payment =>
                payment.PaymentDate)
            .ThenBy(payment =>
                payment.CreationTime)
            .Select(payment =>
                new OrderPaymentDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,

                    PaymentMethod = new LookupDto
                    {
                        Id =
                            (int)payment.PaymentMethod,

                        Name =
                            payment.PaymentMethod
                                .ToString()
                    },

                    PaymentDate =
                        payment.PaymentDate,

                    ReferenceNumber =
                        payment.ReferenceNumber,

                    Note =
                        payment.Note,

                    CreationTime =
                        payment.CreationTime,

                    CreatorId =
                        payment.CreatorId
                })
            .ToList(),

        CreationTime = order.CreationTime,
        CreatorId = order.CreatorId,

        LastModificationTime =
            order.LastModificationTime,

        LastModifierId =
            order.LastModifierId,

        IsDeleted = order.IsDeleted,
        DeleterId = order.DeleterId,
        DeletionTime = order.DeletionTime
    };
}
}