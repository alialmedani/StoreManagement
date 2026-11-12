# Reports Feature — Implementation Guide for the Flutter App

Companion to `MOBILE_HANDOFF.md` (the terse contract reference). This file is written for whoever
builds the "Reports" section in the Store app (`C:\src\APP\store`) and explains not just the shape
of each endpoint but how to use it correctly — what to cache, what to show the user, and which
fields are informational vs. load-bearing.

All 6 endpoints are under `/api/app/report/...`, require `Authorization: Bearer <token>`, and each
has its own permission (403 if missing — check the user's granted permissions before showing the
corresponding screen/tab, same as every other screen in the app).

---

## 1. Sales Summary — `GET /api/app/report/sales-summary`

Permission: `StoreManagement.Reports.SalesSummary`

**Use for:** a dashboard/overview screen — revenue over time, by status, by payment method.

**Request**
```
GET /api/app/report/sales-summary?FromDate=2026-10-01&ToDate=2026-10-31&GroupBy=1
```
| Param | Type | Required | Notes |
|---|---|---|---|
| FromDate | date | yes | |
| ToDate | date | yes | must be `>= FromDate` or you get a 400 |
| GroupBy | int | no, default 1 | `1=Day, 2=Week (Mon start), 3=Month` |
| Status | int | no | `1=Draft, 2=Confirmed, 3=Cancelled`. **Omit this** for normal dashboards — cancelled orders are excluded by default. Only pass `3` if you specifically want a "cancelled orders" view. |
| PaymentStatus | int | no | `1=Unpaid, 2=PartiallyPaid, 3=Paid` |

**Response**
```json
{
  "totalRevenue": 1250000,
  "totalCollected": 980000,
  "totalOrders": 42,
  "buckets": [
    { "periodStart": "2026-10-01", "periodEnd": "2026-10-02", "revenue": 45000, "orderCount": 3 }
  ],
  "byStatus": [ { "status": 2, "orderCount": 40, "revenue": 1250000 } ],
  "byPaymentMethod": [ { "paymentMethod": 1, "amount": 600000, "paymentCount": 20 } ]
}
```

**UI notes**
- `buckets` → feed directly into a line/bar chart, already sorted ascending.
- `status` and `paymentMethod` in `byStatus`/`byPaymentMethod` are **raw ints**, not `{id,name}` objects
  like elsewhere in the app. Map them yourself: Status `1/2/3` → Draft/Confirmed/Cancelled,
  PaymentMethod `1/2/3/4` → Cash/Card/BankTransfer/Other.
- `totalRevenue` (booked on order creation date) and `totalCollected` (booked on payment date) can
  legitimately disagree — don't treat them as the same number with rounding error. Label them
  differently in the UI ("Revenue" vs "Cash Collected").

---

## 2. Inventory Status — `GET /api/app/report/inventory-status`

Permission: `StoreManagement.Reports.InventoryStatus`

**Use for:** "what do I have in stock right now" screen, low-stock alerts.

**Request**
```
GET /api/app/report/inventory-status?LowStockOnly=true&LowStockThreshold=5
```
| Param | Type | Required | Notes |
|---|---|---|---|
| CategoryId | guid | no | filter to one category |
| LowStockOnly | bool | no, default false | |
| LowStockThreshold | int | no, default 5 | variants with `stockQuantity <= threshold` count as low stock |

**Response** — not paged, returns the full snapshot in one call:
```json
{
  "items": [ {
    "productVariantId": "...", "productId": "...", "productName": "T-Shirt",
    "categoryId": "...", "categoryName": "Clothing", "color": "Red", "size": "M",
    "stockQuantity": 3, "price": 15000, "stockValue": 45000,
    "costPrice": 10000, "isOutOfStock": false, "isLowStock": true
  } ],
  "totalStockValue": 2300000, "totalUnits": 540, "variantCount": 48,
  "outOfStockCount": 2, "lowStockCount": 7
}
```

**UI notes**
- `costPrice` can be `null` — only show it (and any cost-based column) when non-null; don't display
  `0` as if it were a real cost.
- This report **always** excludes soft-deleted products/variants — there's no `IncludeDeleted` here
  and there never will be, because it's meant to be live current stock. No action needed on your
  side beyond not expecting a deleted-items toggle on this screen.
- Not paged — if the catalog grows large, consider client-side pagination/virtualized list rather
  than expecting the server to page it.

---

## 3. Top / Bottom Sellers — `GET /api/app/report/top-sellers`

Permission: `StoreManagement.Reports.TopSellers`

**Use for:** "best sellers" / "slow movers" leaderboard screen.

**Request**
```
GET /api/app/report/top-sellers?FromDate=2026-10-01&ToDate=2026-10-31&Top=10&Direction=1&GroupByVariant=false
```
| Param | Type | Required | Notes |
|---|---|---|---|
| FromDate / ToDate | date | yes | |
| GroupByVariant | bool | no | `true` = rank per color/size variant, `false` = rank per product (sums all its variants) |
| Top | int | no, default 10 | 1–200 |
| Direction | int | no | `1=Top sellers, 2=Bottom sellers` |
| Status | int | no | same semantics as Sales Summary — cancelled excluded by default |
| IncludeDeleted | bool | no, default **true** | see § Deleted Records below |

**Response**
```json
{
  "items": [ {
    "productId": "...", "productVariantId": null, "name": "T-Shirt",
    "color": null, "size": null, "quantitySold": 120, "revenue": 1800000,
    "isProductDeleted": false, "isVariantDeleted": false
  } ]
}
```
`productVariantId`/`color`/`size` are `null` when `GroupByVariant=false` (product-level grouping).

---

## 4. Stock Movement Audit — `GET /api/app/report/stock-movements`

Permission: `StoreManagement.Reports.StockMovements`

**Use for:** an audit trail screen — "why did this variant's stock change". This is the only report
that's **paged** like the rest of the app's list screens.

**Request**
```
GET /api/app/report/stock-movements?ProductVariantId=...&skipCount=0&maxResultCount=20&sorting=date desc
```
| Param | Type | Notes |
|---|---|---|
| FromDate / ToDate | date, optional | inclusive |
| ProductVariantId | guid, optional | filter to one variant |
| MovementType | int, optional | `1=Increase, 2=Decrease, 3=Adjustment, 4=Sale, 5=OrderCancellation, 6=Return, 7=OpeningStock` |
| IncludeDeleted | bool, default **true** | |
| sorting | string | `date`/`date desc`/`change`/`change desc` |
| skipCount / maxResultCount | int | standard paging, same as every other list endpoint |

**Response** — standard `{ totalCount, items[] }` shape:
```json
{
  "totalCount": 134,
  "items": [ {
    "id": "...", "date": "2026-10-15T09:30:00",
    "productVariantId": "...", "productVariantName": "T-Shirt (Red / M)",
    "oldQuantity": 10, "change": -2, "newQuantity": 8,
    "reason": { "id": 4, "name": "Sale" },
    "sourceType": { "id": 1, "name": "Order" },
    "referenceId": "order-guid-or-null", "note": null,
    "isProductDeleted": false, "isVariantDeleted": false
  } ]
}
```
`reason`/`sourceType` ARE the normal `{id,name}` lookup shape (unlike Sales Summary's raw ints) —
you can reuse whatever enum-display widget the app already has for `order.status`.

---

## 5. Category Performance — `GET /api/app/report/category-performance`

Permission: `StoreManagement.Reports.CategoryPerformance`

**Request**
```
GET /api/app/report/category-performance?FromDate=2026-10-01&ToDate=2026-10-31
```
Same `FromDate`/`ToDate`/`Status`/`PaymentStatus`/`IncludeDeleted` semantics as the reports above.

**Response**
```json
{
  "items": [ {
    "categoryId": "...", "categoryName": "Clothing",
    "revenue": 3200000, "unitsSold": 210, "orderCount": 85,
    "isCategoryDeleted": false
  } ],
  "totalRevenue": 5100000, "totalUnitsSold": 340
}
```
Pre-sorted by revenue descending — good for a bar chart or ranked list with no extra client sort.

---

## 6. Profit / Margin — `GET /api/app/report/profit-margin`

Permission: `StoreManagement.Reports.ProfitMargin`

**This is the one that needs the most care in the UI.** Read this section fully before building the screen.

**Request**
```
GET /api/app/report/profit-margin?FromDate=2026-10-01&ToDate=2026-10-31&GroupByVariant=false
```
| Param | Type | Notes |
|---|---|---|
| FromDate / ToDate | date, required | |
| CategoryId / ProductId | guid, optional | drill-down filters |
| GroupByVariant | bool | per-variant vs per-product |
| Status | int, optional | cancelled excluded by default |
| IncludeDeleted | bool, default **true** | |

**Response**
```json
{
  "items": [ {
    "productId": "...", "productVariantId": null, "name": "T-Shirt",
    "color": null, "size": null, "quantitySold": 50, "revenue": 750000,
    "costOfGoodsSold": 500000, "grossProfit": 250000, "marginPercent": 33.33,
    "hasCostData": true,
    "isProductDeleted": false, "isVariantDeleted": false,
    "currentCostOfGoodsSold": 600000,
    "hasCostChangedSinceSale": true
  } ],
  "totalRevenue": 750000, "totalCostOfGoodsSold": 500000, "totalGrossProfit": 250000,
  "totalMarginPercent": 33.33, "hasCompleteCostData": true,
  "totalCurrentCostOfGoodsSold": 600000, "hasAnyCostChangedSinceSale": true
}
```

**The core rule: `costOfGoodsSold` / `grossProfit` / `marginPercent` are HISTORICAL and frozen.**
They're computed from the cost recorded at the moment each sale was confirmed, not today's cost.
Changing a variant's cost price today never changes what a past sale's profit was. This is
intentional and matches how real accounting works — do not "fix" a profit number that looks stale,
it's correct.

**`hasCostData` — check this before showing a percentage.**
- `false` means at least one sold unit in that row has no recorded historical cost (this happens for
  any sale confirmed before this feature existed, or a variant that never had a cost price set).
  Its `costOfGoodsSold` is `0` for that portion, which means `grossProfit`/`marginPercent` are
  **understated**, not accurate. Show a "cost data incomplete" badge instead of a bare percentage —
  do not present it as a trustworthy margin.
- `hasCompleteCostData` (top level) is the same check across the whole report — use it to show a
  banner like "some items are missing cost data" at the top of the screen.

**`currentCostOfGoodsSold` / `hasCostChangedSinceSale` — informational only, never substitute these for the historical fields.**
- These tell you what the COGS *would be* if everything were re-priced at today's cost. Use them only
  for a secondary, clearly-labeled hint like "cost has changed since this sale" — e.g. the example
  from the original request: show "Historical profit: 5,000" as the primary number, and separately
  "current cost is 12,000 (was 10,000 at sale time)" as a footnote. Never add these into the main
  profit calculation or let them replace `grossProfit`.
- `currentCostOfGoodsSold` is `null` when it can't be computed for every row (e.g. a variant has since
  been hard-deleted's cost is unknown — in practice: when any row's current cost is unavailable).
  Treat `null` the same as "don't show this footnote."

---

## Deleted Records (`IncludeDeleted` + `is*Deleted` flags)

Applies to: Top Sellers, Stock Movements, Category Performance, Profit Margin. **Does not apply** to
Sales Summary (doesn't need it) or Inventory Status (always excludes deleted, no toggle).

- **Default is `IncludeDeleted=true`** — the opposite of how soft-delete normally works in this app
  (lists elsewhere default to excluding deleted). This is deliberate: a discontinued product's past
  sales must still count in historical totals. **Don't pass `IncludeDeleted=false` by default** or
  your revenue totals will silently shrink whenever something gets discontinued.
- Pass `IncludeDeleted=false` only for an explicit "current catalog only" toggle, if you want to offer
  one (e.g. a checkbox: "☐ Include discontinued products").
- When a row has `isProductDeleted` / `isVariantDeleted` / `isCategoryDeleted` = `true`, the name
  fields still show the entity's original name (soft-delete doesn't erase data) — show a small
  "Discontinued" / "Deleted" chip next to the name rather than hiding or graying out the whole row.
- A soft-deleted **order** or **stock movement itself** can never appear in any report, regardless of
  `IncludeDeleted` — that flag only ever affects whether *catalog* references (product/variant/category)
  are resurrected for display, never transactional records. You don't need to do anything differently
  here; just know you'll never see a "deleted order" sneak into a report.

---

## Suggested screen breakdown

| Screen | Endpoint(s) |
|---|---|
| Dashboard / Overview | Sales Summary |
| Inventory | Inventory Status |
| Best/Worst Sellers | Top Sellers |
| Stock Audit Log | Stock Movements (paged list) |
| Category Breakdown | Category Performance |
| Profit & Margin | Profit Margin (with the cost-data caveats above) |

## Checklist before shipping each screen
- [ ] Permission check gates screen visibility (403 → hide tab, don't crash)
- [ ] Date range picker enforces `ToDate >= FromDate` client-side (saves a round trip)
- [ ] Raw-int enums (Sales Summary's `status`/`paymentMethod`) mapped to labels, not shown as numbers
- [ ] `hasCostData`/`hasCompleteCostData` gate whether a margin % is shown as trustworthy
- [ ] `is*Deleted` rows get a visible badge, not hidden
- [ ] `IncludeDeleted` left at default (`true`) unless the user explicitly asked for "current only"
