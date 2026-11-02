# StoreManagement Backend — Agent Fleet

| Agent | Role | Writes code? |
|---|---|---|
| **store-backend-brain** | Orchestrator: plans, enforces layer/contract rules, delegates, verifies, updates mobile handoff + roster | coordinates |
| **store-backend-dev** | Implements entities, services, DTOs, permissions, EF mappings, migrations, localization | yes |
| **store-orders-inventory-expert** | Owns Orders/Payments/StockMovement invariants and OrderManager/InventoryManager | yes (domain) |
| **store-backend-reviewer** | Read-only review: layering, auth, errors, localization, migrations, contract breaks | no |

Flow: brain → (expert for orders/stock) → dev → reviewer → brain updates `docs/MOBILE_HANDOFF.md`.
Source of truth: `CLAUDE.md`, `.abpstudio/ai-rules/*.mdc`. Mobile counterpart: `C:\src\APP\store\.claude\agents`.
Maintenance: change a convention/route/permission/error code → update the agent file(s) and the handoff doc in the same task.
