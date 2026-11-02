# CLAUDE.md — StoreManagement (ABP backend)

Read this first. It binds every agent and session in this repo.

## What this is
ABP Framework (DDD, layered) .NET solution for a clothing/shoes store: Categories, Products, ProductVariants
(SKU, size/color, stock), Inventory (StockMovement ledger), Orders (items, payments, confirm/cancel), Media.
Consumer: the Flutter app at `C:\src\APP\store` (its agents: `C:\src\APP\store\.claude\agents`).

## Layout (`src/`)
Domain.Shared (enums, consts, localization, error codes) · Domain (entities, domain services, repositories interfaces) ·
Application.Contracts (DTOs, service interfaces, permissions) · Application (AppServices, Mapperly mappers, validators) ·
EntityFrameworkCore (DbContext, mappings, migrations) · HttpApi / Web (host, OpenIddict, auth, exception→HTTP mapping) ·
DbMigrator (migrations + seed). `.abpstudio/ai-rules/*.mdc` has ABP's own coding rules - follow them too.

## API shape (the mobile app depends on it - treat as a public contract)
Conventional controllers: `/api/app/<kebab-service>`; GetList→GET, Get→GET `{id}`, Create→POST, Update→PUT `{id}`,
Delete→DELETE `{id}`, `GetDeletedListAsync`→GET `deleted-list`, `RestoreAsync`→POST `{id}/restore`,
Order: `confirm|cancel|record-payment` (POST `{id}/…`), items `POST {id}/item`, `PUT/DELETE {id}/item/{itemId}`.
Auth: OpenIddict, client `StoreManagement_Mobile`, password grant, scopes incl. `offline_access StoreManagement`.

## STRICT RULES
1. **Business rules live in the Domain** (entity methods / domain services `OrderManager`, `InventoryManager`), never in
   AppServices or controllers. Entities use private setters + guarded methods; no anemic mutation from outside.
2. **Errors:** throw `BusinessException` with a code constant in `StoreManagementDomainErrorCodes` (`StoreManagement:xxxxx`),
   a message in BOTH `Localization/StoreManagement/en.json` AND `ar.json`, and an HTTP status mapping in the Web module
   (`ConfigureExceptionHttpStatusCodes`). Never throw bare `Exception`/`InvalidOperationException` for business cases,
   never use string literals for codes.
3. **Every AppService method is authorized.** Class-level `[Authorize]` plus a permission from `StoreManagementPermissions`
   (define it, register in `PermissionDefinitionProvider`, add localization). New endpoints must NOT ship anonymous.
   (Known gap: some existing read endpoints lack `[Authorize]` - see Known debt.)
4. **Stock integrity:** every stock change goes through `InventoryManager` and writes a `StockMovement` with
   `old + change = new`; never edit `ProductVariant` stock directly; no negative stock; order confirm/cancel must keep stock and
   movements consistent inside one unit of work.
5. **DTOs & validation:** DTOs in Application.Contracts; input validated with FluentValidation validators (or data annotations
   consistent with neighbours); never expose entities. Paged lists use `PagedAndSortedResultRequestDto` / `PagedResultDto<T>`.
6. **Mapping:** Mapperly (source-generated) like existing mappers - NOT AutoMapper.
7. **Soft delete:** deletable entities implement `ISoftDelete`; provide `GetDeletedListAsync` + `RestoreAsync` the same way
   Category/Product/ProductVariant/Order do. Hard delete only if explicitly asked.
8. **Database:** change entity → update EF mapping → `dotnet ef migrations add <Name>` (EntityFrameworkCore project) →
   test through `DbMigrator`. Never edit an applied migration; never hand-edit the snapshot. Money = `decimal(18,2)` configured.
9. **API contract stability:** do not rename routes, DTO properties, or change enum numeric values consumed by the mobile app
   without updating `docs/MOBILE_HANDOFF.md` and telling the user the mobile app must change too. Add, don't break.
10. **Secrets:** no passwords, certificates, connection strings with credentials, or API keys in code, agent files or docs.
    Use user-secrets/env/appsettings placeholders. (Existing seed admin password is dev-only; never reuse it elsewhere.)
11. **Localization everywhere:** any new message/permission/enum display text needs en + ar.
12. **Tests:** add/adjust tests in `test/` for domain rules you change when a test project is present for that layer.
13. **Honest reporting:** say what was built, what was run (`dotnet build`, `dotnet test`, migrations) and what was NOT. Never claim a
    green build you did not see. If `dotnet` is unavailable in the sandbox, say "not built".
14. Scope discipline: minimal diffs, match neighbouring style, no new packages without asking.
15. Talk to the user in their language (usually Arabic); code/comments/commits in English.

## Agents (`.claude/agents/`)
Entry: **`store-backend-brain`** → `store-backend-dev`, `store-orders-inventory-expert`, `store-backend-reviewer`.
Any change to a convention, route, permission, error code or DTO that the app consumes must update the agent files AND
`docs/MOBILE_HANDOFF.md` in the same task.

## Known debt (report, don't silently fix unless asked)
- Several read endpoints (Category/Product/ProductVariant lists/gets) are reachable without `[Authorize]`.
- `ar.json` error localization is incomplete vs `en.json`; some code paths use string-literal error codes instead of constants.
- Dev seed credentials exist for the admin user; the mobile app hardcodes them (mobile debt).
