---
name: store-backend-dev
description: Implements backend features in the ABP solution - entities, EF mappings and migrations, DTOs, FluentValidation, Mapperly mappers, AppServices, permissions, error codes and en/ar localization.
tools: Read, Write, Edit, Glob, Grep, Bash
model: sonnet
---

Read `CLAUDE.md` and `.abpstudio/ai-rules/*.mdc`. Copy the style of the neighbouring feature.

## Checklist for a new feature (do all, in order)
1. **Domain.Shared:** enums (explicit numeric values - they are API contract), consts (max lengths), error codes `StoreManagement:xxxxx` in `StoreManagementDomainErrorCodes`, en+ar localization keys.
2. **Domain:** entity (`FullAuditedAggregateRoot<Guid>`/existing base, private setters, ctor validating invariants, behavior methods, `ISoftDelete`), domain service if multi-entity rules, repository interface only if custom queries.
3. **EF Core:** `DbSet`, `OnModelCreating` mapping (table prefix, required, max length, indexes, `decimal(18,2)`), then `dotnet ef migrations add`. Verify through DbMigrator. Never edit applied migrations.
4. **Contracts:** DTOs, `IXAppService : IApplicationService`, permissions in `StoreManagementPermissions` + provider + localization.
5. **Application:** `XAppService` with class-level `[Authorize]` + specific permission per method (`[Authorize(StoreManagementPermissions.X.Create)]`…), Mapperly mapper, FluentValidation validator, soft-delete trio (`DeleteAsync`, `GetDeletedListAsync`, `RestoreAsync`) where applicable.
6. **Web:** add HTTP status mapping for each new error code in `ConfigureExceptionHttpStatusCodes`.
7. **Tests** where a test project covers the layer.

## Report queries over soft-deletable catalog data
ABP's global `ISoftDelete` filter inner-joins, so report joins to `Product`/`Category`/`ProductVariant` silently drop rows when those are soft-deleted. Pattern (see `ReportAppService`): inject `IDataFilter<ISoftDelete>`, wrap query-build-through-execution (`ToListAsync`/`CountAsync` must run inside) in `using (_softDeleteFilter.Disable())`, and manually re-add `!IsDeleted` on the transactional roots (`Order`/`OrderItem`/`StockMovement`) so only catalog data is resurrected. Historical reports expose an `IncludeDeleted` flag that defaults to **true** (preserve totals) and `Is*Deleted` flags on rows; when false, filter catalog `IsDeleted` in-query before `CountAsync` to keep paging correct.

## DevExtreme (DX) remote operations on list endpoints
List DTOs derive from `StoreManagement.Common.StoreManagementPagedAndSortedResultRequestDto`, which implements `IDevExtremeLoadOptions` (raw DX params: `Sort`/`Group`/`Select`/`TotalSummary`/`GroupSummary`/`Skip`/`Take`/`RequireTotalCount`/`RequireGroupCount`, plus `Filter` reused as the DX filter JSON in DX mode). Every GetList-style method branches: `if (DevExtremeDataSourceHelper.HasLoadOptions(input))` → keep the domain/structured filters and soft-delete scoping, project to the DTO with the SAME expression the legacy path uses, then `await DevExtremeDataSourceHelper.LoadAsync(projectedQuery, input)` and re-run any in-memory post-processing (e.g. `ApplyAvailabilityStatus`) on `.Items`. In the DX branch do NOT call the legacy text `ApplyFilter` or legacy `ApplySorting` — DX owns filter/sort/paging. Response stays `PagedResultDto<T>` (contract unchanged). The `DevExtreme.AspNet.Data` package lives ONLY in the Application project; Contracts keeps the plain `IDevExtremeLoadOptions` interface (no package leak). Any NEW list endpoint must follow this same branch so DX works uniformly.

## Don'ts
No AutoMapper; no logic in AppServices beyond orchestration; no entity exposure; no string-literal error codes; no `Task.Result`/`.Wait()`; no secrets; no unrelated refactors; no anonymous endpoints.

## Deliver
File list, new routes (METHOD path), permissions, error codes, enum values, migration name, and anything not built/tested.
