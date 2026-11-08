# Proposal: Sales Summary Report Endpoint

Status: **proposal, not implemented.** Written to support a new "Reports" section in the
Store mobile app (`C:\src\APP\store`). No report/analytics AppService exists in the backend
today — this is new surface area, not a gap-fill on an existing one.

## Why this is needed

Investigated the current backend (`C:\src\BACK\StoreManagement`) and confirmed:

- No `*ReportAppService`, `*AnalyticsAppService`, `*DashboardAppService`, or
  `*StatisticsAppService` exists anywhere in `src/`.
- No permission group for reports exists (`StoreManagementPermissions` only has
  `Categories`, `Products`, `ProductVariants`, `Inventory`, `Orders`, `File`, `ClaimManagement`).
- `OrderAppService.GetListAsync` (`src/StoreManagement.Application/Orders/OrderAppService.cs`)
  supports `FromDate`/`ToDate`/`Status`/`PaymentStatus` filters but returns a flat paged list —
  no grouping or summation across orders anywhere in the codebase.
- `PaymentMethod` lives only on the `OrderPayment` sub-entity, reachable only per-order via
  `GetAsync(id)` — not present on the list DTO (`OrderDto`) at all. A payment-method breakdown
  is not buildable client-side without N+1 calls.
- Dead, unwired code exists suggesting a similar feature was once planned but abandoned:
  `OrderPaymentSummaryDto` and `OrderPaymentPagedRequestDto`
  (`src/StoreManagement.Application.Contracts/Orders/`) — referenced nowhere else in the
  codebase. Worth checking with whoever wrote those before building this fresh.

Per `CLAUDE.md` mobile rule 1 ("never invent an endpoint, DTO field, enum value or response
shape"), the mobile app cannot build this report until a real backend contract exists.

## Proposed endpoint

`GET /api/app/order/sales-summary` (ABP conventional auto-API naming for a new method on
`IOrderAppService`, or a new dedicated `IReportAppService` if more report endpoints are
expected later — recommend the latter so permissions/versioning aren't tangled with `Orders`).

### Request: `SalesSummaryRequestDto`

```csharp
public class SalesSummaryRequestDto
{
    public DateTime FromDate { get; set; }   // required
    public DateTime ToDate { get; set; }     // required
    public SalesSummaryGroupBy GroupBy { get; set; } = SalesSummaryGroupBy.Day;
    public OrderStatus? Status { get; set; }               // optional filter
    public OrderPaymentStatus? PaymentStatus { get; set; } // optional filter
}

public enum SalesSummaryGroupBy
{
    Day = 1,
    Week = 2,
    Month = 3
}
```

### Response: `SalesSummaryDto`

```csharp
public class SalesSummaryDto
{
    public decimal TotalRevenue { get; set; }     // sum of Order.TotalAmount in range
    public decimal TotalCollected { get; set; }   // sum of OrderPayment.Amount in range (by PaymentDate)
    public int TotalOrders { get; set; }

    public List<SalesSummaryBucketDto> Buckets { get; set; }
    public List<SalesSummaryStatusBreakdownDto> ByStatus { get; set; }
    public List<SalesSummaryPaymentMethodBreakdownDto> ByPaymentMethod { get; set; }
}

public class SalesSummaryBucketDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Revenue { get; set; }      // Order.TotalAmount bucketed by CreationTime
    public int OrderCount { get; set; }
}

public class SalesSummaryStatusBreakdownDto
{
    public OrderStatus Status { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesSummaryPaymentMethodBreakdownDto
{
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }   // bucketed by OrderPayment.PaymentDate
    public int PaymentCount { get; set; }
}
```

## Aggregation semantics

- `Revenue` / `Buckets` / `ByStatus` are computed from `Order.TotalAmount`, bucketed on
  `Order.CreationTime`, filtered by the request's `Status`/`PaymentStatus` if given — same
  filter semantics `OrderAppService.GetListAsync` already uses for `FromDate`/`ToDate`.
- `ByPaymentMethod` / `TotalCollected` are computed from `OrderPayment.Amount`, bucketed on
  `OrderPayment.PaymentDate` (not the parent order's `CreationTime`) — these two sides can
  legitimately disagree (e.g. an order confirmed this month but paid off next month).
- Default behavior for `Cancelled` orders in revenue totals needs an explicit decision from
  whoever implements this — likely excluded by default unless `Status` filter says otherwise.
- `Buckets` requires `GROUP BY` on a date-truncated `CreationTime` (day/week/month). No such
  pattern exists in the codebase today (confirmed by investigation) — this is new aggregation
  logic, not a copy of an existing method.

## Permission

Add `StoreManagementPermissions.Orders.SalesSummary`, or a new `Reports` permission group if
more report endpoints are planned beyond this one. No `Reports` group currently exists in
`src/StoreManagement.Application.Contracts/Permissions/StoreManagementPermissions.cs`.

## Key files referenced during investigation

- `src/StoreManagement.Domain/Orders/Order.cs`
- `src/StoreManagement.Domain/Orders/OrderPayment.cs`
- `src/StoreManagement.Domain.Shared/Orders/OrderStatus.cs`
- `src/StoreManagement.Domain.Shared/Orders/OrderPaymentStatus.cs`
- `src/StoreManagement.Domain.Shared/Orders/PaymentMethod.cs`
- `src/StoreManagement.Application/Orders/OrderAppService.cs`
- `src/StoreManagement.Application.Contracts/Orders/IOrderAppService.cs`
- `src/StoreManagement.Application.Contracts/Orders/OrderPagedRequestDto.cs`
- `src/StoreManagement.Application.Contracts/Orders/OrderDto.cs`
- `src/StoreManagement.Application.Contracts/Orders/OrderPaymentSummaryDto.cs` (unused)
- `src/StoreManagement.Application.Contracts/Orders/OrderPaymentPagedRequestDto.cs` (unused)
- `src/StoreManagement.Application.Contracts/Permissions/StoreManagementPermissions.cs`
