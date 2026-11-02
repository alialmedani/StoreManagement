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
Lists: query `filter, sorting, skipCount, maxResultCount` → `{ totalCount, items[] }`. DELETE/restore/action endpoints may return 2xx with an empty body.

## Errors
ABP envelope: `{ "error": { "code", "message", "details", "validationErrors": [...] } }`. Business codes `StoreManagement:xxxxx` (see `StoreManagementDomainErrorCodes.cs`); messages localized en/ar by `Accept-Language`.

## Change log (newest first)
- Initial handoff written; mobile now covers order actions, delete/restore, deleted-items screen.

## Open backend gaps affecting the app
- Some read endpoints are not `[Authorize]`d (security; app already sends tokens, so adding auth is non-breaking).
- ar.json lacks some error messages present in en.json → Arabic users may see English errors.
