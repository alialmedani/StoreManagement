# Mobile Handoff — API contract consumed by the Flutter app

Maintained by `store-backend-brain`. Update in the SAME task as any change to routes, DTOs, enums, permissions or error codes.
Swagger (`/swagger/v1/swagger.json`) is authoritative; this file is the human summary. Routes below follow ABP conventions
(verify custom verbs in Swagger when the backend runs).

## Auth
- `POST /connect/token` — `grant_type=password`, `client_id=StoreManagement_Mobile`, scope `offline_access StoreManagement`; refresh with `grant_type=refresh_token`.
- Send `Authorization: Bearer <access_token>`. 401 = unauthenticated, 403 = missing permission.

## Endpoints (base `/api/app`)
| Area | Operations |
|---|---|
| `category` | GET list · GET `{id}` · POST · PUT `{id}` · DELETE `{id}` · GET `deleted-list` · POST `{id}/restore` |
| `product` | same set |
| `product-variant` | same set |
| `order` | GET list · GET `{id}` (includes items + payments) · POST · PUT `{id}` · DELETE `{id}` · GET `deleted-list` · POST `{id}/restore` · POST `{id}/confirm` · POST `{id}/cancel` · POST `{id}/record-payment` · POST `{id}/item` · PUT `{id}/item/{itemId}` · DELETE `{id}/item/{itemId}` |
| `report` | GET `sales-summary` · GET `inventory-status` · GET `top-sellers` · GET `stock-movements` · GET `category-performance` · GET `profit-margin` (all query params, see below) |
Lists: query `filter, sorting, skipCount, maxResultCount` → `{ totalCount, items[] }`. DELETE/restore/action endpoints may return 2xx with an empty body. Every GetList-style endpoint also accepts optional DevExtreme remote-operations params (additive) — see "DevExtreme (DX) remote operations" below.

## DevExtreme (DX) remote operations (optional, additive)
Every GetList-style endpoint (`category` list + `deleted-list`, `category/stock-summary`, `product` list + `deleted-list`, `product-variant` list + `deleted-list` + `by-product`, `order` list, `stock-movement`/inventory list, `report/stock-movements`) now ALSO understands the standard DevExtreme DataGrid/DataSource remote-operations query params. These are 100% additive: nothing changed for existing callers. The legacy params (`filter`, `sorting`, `skipCount`, `maxResultCount`) keep working exactly as before, and the response envelope is unchanged — still `PagedResultDto<T>` = `{ totalCount, items[] }` (DX's `data`/`totalCount` are mapped into that same shape).

New optional query params (all nullable):
- `sort` — DX sort JSON, e.g. `[{"selector":"name","desc":true}]`
- `group` — DX group JSON
- `select` — DX select JSON
- `totalSummary` — DX total-summary JSON
- `groupSummary` — DX group-summary JSON
- `skip` (int) — DX paging offset
- `take` (int) — DX page size
- `requireTotalCount` (bool)
- `requireGroupCount` (bool)
- `filter` — REUSES the existing `filter` query key. In legacy mode it is the plain free-text search string (unchanged); in DX mode it is the DX filter JSON, e.g. `[["name","contains","abc"]]`.

Mode switch: DX mode engages only when at least one DX-ONLY param is present (`sort`/`group`/`select`/`totalSummary`/`groupSummary`/`skip`/`take`/`requireTotalCount`/`requireGroupCount`). Sending `filter` alone does NOT trigger DX mode (so the existing plain-text filter behaviour is preserved). In DX mode the server ignores `sorting`/`skipCount`/`maxResultCount` and lets DX own filtering/sorting/paging; domain/structured query params (e.g. `categoryId`, `status`, `paymentStatus`, `fromDate`/`toDate`, `availabilityStatus`, `productVariantId`, `movementType`, `includeDeleted`) still apply alongside the DX filter.
- DX field names target the DTO property names returned in `items` (e.g. `product` list: `name`, `stockQuantity`; `report/stock-movements`: the flat movement-row fields such as `date`, `productName`, `color`, `size`, `oldQuantity`, `change`, `newQuantity`). `category/stock-summary` runs DX in-memory over the computed summary list.
- DX `filter`/`sort`/`select` must target RAW scalar fields only. Do NOT DX-filter or DX-sort on computed/composite fields — in particular `availabilityStatus` (a `{id,name}` LookupDto computed in-memory AFTER the DB query, on `product`/`product-variant` lists): it is not part of the translated query and a DX `sort`/`filter` on it will error. Use the underlying `stockQuantity` instead (e.g. DX filter `[["stockQuantity","<=",0]]` for out-of-stock). To narrow by availability keep using the existing structured `availabilityStatus` query param, which still applies in DX mode.
- The response stays flat `{ totalCount, items[] }`; server-side grouping/summary beyond `totalCount` is not reshaped into the envelope. `group`/`groupSummary` are accepted (a grid may send them) but ignored for shaping — you always get a flat page, never grouped nodes.

The Flutter app does NOT need to change. Adopt these params only where you want server-side DX grid behaviour.

## Product variant — new optional field `costPrice`
Additive, non-breaking. `product-variant` Create (POST) and Update (PUT `{id}`) now accept an optional `costPrice` (decimal, nullable, `>= 0`); `ProductVariantDto` now returns `costPrice` (nullable decimal). Omit it to leave cost unset. Historical variants and variants created via bulk/generate endpoints have `costPrice = null` until updated; profit-margin reporting flags such rows (`hasCostData = false`). No existing field changed.

## Sales summary report
`GET /api/app/report/sales-summary` — requires permission `StoreManagement.Reports.SalesSummary` (403 otherwise).

Query params (`SalesSummaryRequestDto`):
- `FromDate` (date, required), `ToDate` (date, required). Validation: `ToDate >= FromDate` (else 400 validation error). Range is inclusive of both days (compared as `[FromDate 00:00, ToDate+1day)`).
- `GroupBy` (int enum, default `1`): `Day=1`, `Week=2` (weeks start Monday), `Month=3`.
- `Status` (int enum, optional): `Draft=1`, `Confirmed=2`, `Cancelled=3`. **Cancelled orders are excluded from all revenue figures by default** unless you explicitly pass `Status=3`.
- `PaymentStatus` (int enum, optional): `Unpaid=1`, `PartiallyPaid=2`, `Paid=3`.

Response (`SalesSummaryDto`):
```
{
  "totalRevenue": decimal,        // sum of Order.TotalAmount in range (CreationTime)
  "totalCollected": decimal,      // sum of OrderPayment.Amount in range (PaymentDate)
  "totalOrders": int,
  "buckets": [                    // revenue grouped by GroupBy period, ascending by periodStart
    { "periodStart": date, "periodEnd": date, "revenue": decimal, "orderCount": int }
  ],
  "byStatus": [                   // raw int OrderStatus, NOT the {id,name} LookupDto used elsewhere
    { "status": 2, "orderCount": int, "revenue": decimal }
  ],
  "byPaymentMethod": [            // raw int PaymentMethod: Cash=1, Card=2, BankTransfer=3, Other=4
    { "paymentMethod": 1, "amount": decimal, "paymentCount": int }
  ]
}
```
Note: `byStatus.status` and `byPaymentMethod.paymentMethod` serialize as raw integer enums (per the report spec), unlike `order`'s `status`/`paymentStatus` which are `LookupDto {id,name}`. The revenue side (`buckets`, `byStatus`, `totalRevenue`) is bucketed on `Order.CreationTime`; the collected side (`byPaymentMethod`, `totalCollected`) is bucketed on `OrderPayment.PaymentDate`, so the two sides can legitimately differ for a given period.

## Inventory status report
`GET /api/app/report/inventory-status` — permission `StoreManagement.Reports.InventoryStatus`.
Query (`InventoryStatusRequestDto`): `CategoryId` (guid, optional), `LowStockOnly` (bool, default false), `LowStockThreshold` (int, default `5`, `>= 0`). When `LowStockOnly=true`, only variants with `stockQuantity <= LowStockThreshold` (includes out-of-stock) are returned.
Response (`InventoryStatusDto`):
```
{
  "items": [ {
    "productVariantId": guid, "productId": guid, "productName": string,
    "categoryId": guid, "categoryName": string, "color": string, "size": string,
    "stockQuantity": int, "price": decimal,       // price = Product.Price (variants have no own price)
    "stockValue": decimal,                        // stockQuantity * price
    "costPrice": decimal|null,
    "isOutOfStock": bool, "isLowStock": bool
  } ],
  "totalStockValue": decimal, "totalUnits": int, "variantCount": int,
  "outOfStockCount": int, "lowStockCount": int
}
```
Not paged (full snapshot). Current stock is read from the variant's live `StockQuantity`, not reconstructed from the ledger.

## Top sellers report
`GET /api/app/report/top-sellers` — permission `StoreManagement.Reports.TopSellers`.
Query (`TopSellersRequestDto`): `FromDate`/`ToDate` (date, required, `ToDate >= FromDate`), `GroupByVariant` (bool; true = per variant, false = per product), `Top` (int, default `10`, 1..200), `Direction` (int enum `Top=1`, `Bottom=2`), `Status` (int enum optional; cancelled excluded by default unless `Status=3`), `IncludeDeleted` (bool, **default `true`** — see "Soft-deleted rows in reports" below). Computed from order items on `Order.CreationTime`.
Response (`TopSellersDto`): `{ "items": [ { "productId": guid, "productVariantId": guid|null, "name": string, "color": string|null, "size": string|null, "quantitySold": int, "revenue": decimal, "isProductDeleted": bool, "isVariantDeleted": bool } ] }`. `productVariantId`/`color`/`size` are null when `GroupByVariant=false`; `isVariantDeleted` is only meaningful when `GroupByVariant=true` (false otherwise).

## Stock movements report
`GET /api/app/report/stock-movements` — permission `StoreManagement.Reports.StockMovements`. Paged read-only ledger view.
Query (`ReportStockMovementRequestDto` : paged): `FromDate`/`ToDate` (date, optional, inclusive), `ProductVariantId` (guid, optional), `MovementType` (int enum optional: `Increase=1, Decrease=2, Adjustment=3, Sale=4, OrderCancellation=5, Return=6, OpeningStock=7`), `IncludeDeleted` (bool, **default `true`**), plus `sorting` (`date`/`change`), `skipCount`, `maxResultCount`.
Response `PagedResultDto<ReportStockMovementDto>`: `{ totalCount, items: [ { "id": guid, "date": datetime, "productVariantId": guid, "productVariantName": "Name (Color / Size)", "oldQuantity": int, "change": int, "newQuantity": int, "reason": {id,name}, "sourceType": {id,name}, "referenceId": guid|null, "note": string|null, "isProductDeleted": bool, "isVariantDeleted": bool } ] }`. `reason`/`sourceType` are `LookupDto {id,name}` (enum). `referenceId` points to the related order when `sourceType` is order-driven.

## Category performance report
`GET /api/app/report/category-performance` — permission `StoreManagement.Reports.CategoryPerformance`.
Query (`CategoryPerformanceRequestDto`): `FromDate`/`ToDate` (date, required), `Status` (int enum optional; cancelled excluded by default), `PaymentStatus` (int enum optional), `IncludeDeleted` (bool, **default `true`**).
Response (`CategoryPerformanceDto`): `{ "items": [ { "categoryId": guid, "categoryName": string, "revenue": decimal, "unitsSold": int, "orderCount": int, "isCategoryDeleted": bool } ], "totalRevenue": decimal, "totalUnitsSold": int }`. `orderCount` = distinct orders with at least one item in that category. Items sorted by revenue desc.

## Profit margin report
`GET /api/app/report/profit-margin` — permission `StoreManagement.Reports.ProfitMargin`.
Query (`ProfitMarginRequestDto`): `FromDate`/`ToDate` (date, required), `CategoryId` (guid, optional), `ProductId` (guid, optional), `GroupByVariant` (bool), `Status` (int enum optional; cancelled excluded by default), `IncludeDeleted` (bool, **default `true`**).
Response (`ProfitMarginDto`):
```
{
  "items": [ {
    "productId": guid, "productVariantId": guid|null, "name": string, "color": string|null, "size": string|null,
    "quantitySold": int, "revenue": decimal,
    "costOfGoodsSold": decimal,     // HISTORICAL: sum(qty * CostPriceAtSale snapshot); rows with no snapshot contribute 0
    "grossProfit": decimal, "marginPercent": decimal,
    "hasCostData": bool,            // false when any sold unit lacks a cost SNAPSHOT → cost/margin understated
    "isProductDeleted": bool, "isVariantDeleted": bool,   // isVariantDeleted only meaningful when GroupByVariant=true
    "currentCostOfGoodsSold": decimal|null,   // informational only: sum(qty * variant's CURRENT costPrice); null unless every row has a current cost. Never feeds costOfGoodsSold/grossProfit/marginPercent.
    "hasCostChangedSinceSale": bool           // true if the snapshot and current cost differ for any row
  } ],
  "totalRevenue": decimal, "totalCostOfGoodsSold": decimal, "totalGrossProfit": decimal,
  "totalMarginPercent": decimal, "hasCompleteCostData": bool,
  "totalCurrentCostOfGoodsSold": decimal|null, "hasAnyCostChangedSinceSale": bool
}
```
IMPORTANT SEMANTIC CHANGE: `costOfGoodsSold`/`grossProfit`/`marginPercent` are now sourced from the per-sale cost SNAPSHOT (`CostPriceAtSale`, captured once when the order is confirmed), not the variant's live `costPrice`. Orders confirmed BEFORE this feature shipped have a null snapshot, so their `costOfGoodsSold=0` and `hasCostData=false` for that row/group. Rely on `hasCostData` to caveat or hide margin figures rather than showing a misleading 100% margin — this is unchanged advice, but the underlying cause is now "no snapshot" rather than "no live cost". `currentCostOfGoodsSold`/`hasCostChangedSinceSale` are purely informational and must not be used in place of the historical fields. No pre-existing field changed type.

## Soft-deleted rows in reports (top-sellers, category-performance, profit-margin, stock-movements)
These 4 endpoints each accept an optional `IncludeDeleted` query param (bool) that **defaults to `true`** — this is the OPPOSITE of ABP's usual default-false soft-delete behavior, done deliberately so historical totals stay stable even after a product/category/variant is later soft-deleted. Pass `IncludeDeleted=false` to drop rows tied to soft-deleted catalog records (useful for "current catalog only" views). Soft-deleted orders/order-items/stock-movements themselves are always excluded regardless of this flag; `IncludeDeleted` only controls whether rows referencing a soft-deleted Product/Category/ProductVariant are kept. `sales-summary` and `inventory-status` were reviewed and are UNCHANGED by this work (sales-summary joins no deletable catalog entity; inventory-status intentionally always excludes soft-deleted variants by design).

## Product variant cost history — backend-only, not exposed
`ProductVariantCostHistory` (an audit trail written automatically whenever a variant's `costPrice` is created/changed) is intentionally NOT exposed via any mobile endpoint in this change. Decision: no clear mobile need yet, and exposing it would require a new permission; it can be added as a read-only endpoint later if the mobile app needs a cost-change audit view.

## Errors
ABP envelope: `{ "error": { "code", "message", "details", "validationErrors": [...] } }`. Business codes `StoreManagement:xxxxx` (see `StoreManagementDomainErrorCodes.cs`); messages localized en/ar by `Accept-Language`.

## Change log (newest first)
- Report endpoints `top-sellers`/`category-performance`/`profit-margin`/`stock-movements` gained an optional `IncludeDeleted` query param (default `true`, additive) plus new `is*Deleted` response flags, fixing a bug where rows tied to a soft-deleted Product/Category/ProductVariant were silently dropped. `profit-margin`'s `costOfGoodsSold`/`grossProfit`/`marginPercent` now source from the per-sale `CostPriceAtSale` snapshot (historical) instead of the live variant cost; new informational `currentCostOfGoodsSold`/`hasCostChangedSinceSale` (item) and `totalCurrentCostOfGoodsSold`/`hasAnyCostChangedSinceSale` (totals) fields added. No existing field removed or retyped. `sales-summary`/`inventory-status` unchanged.
- Added 5 report endpoints under `report`: `inventory-status`, `top-sellers`, `stock-movements`, `category-performance`, `profit-margin`, each with its own permission (`StoreManagement.Reports.InventoryStatus` / `.TopSellers` / `.StockMovements` / `.CategoryPerformance` / `.ProfitMargin`). Additive.
- Added optional `costPrice` (nullable decimal) to `product-variant` Create/Update and to `ProductVariantDto` (DB column added via `AddCostPriceToProductVariant` migration). Additive; feeds the profit-margin report.
- Added `GET /api/app/report/sales-summary` (new `report` service + `StoreManagement.Reports.SalesSummary` permission). Additive; no existing contract changed.
- Initial handoff written; mobile now covers order actions, delete/restore, deleted-items screen.

## Open backend gaps affecting the app
- Some read endpoints are not `[Authorize]`d (security; app already sends tokens, so adding auth is non-breaking).
- ar.json lacks some error messages present in en.json → Arabic users may see English errors.
