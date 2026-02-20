# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Neighborhood Tool Co-op** — A multi-tenant SaaS platform for managing shared tool inventory (check-out/return, reservations, QR code labels). Built with .NET 8, Blazor WASM (hosted model), Dapper + PostgreSQL.

## Common Commands

### Start Local Database
```bash
docker-compose up -d
```
PostgreSQL v16 on `localhost:5432`. Credentials are in `docker-compose.yml`.

### Run the Application
```bash
dotnet run --project src/SpearSoft.NeighborhoodToolCoop.Server
```
Serves at `http://localhost:5164` / `https://localhost:7077`. The Server also hosts the compiled Blazor WASM client. DbUp migrations run automatically on startup.

### Build
```bash
dotnet build
dotnet build -c Release
```

### Publish
```bash
dotnet publish src/SpearSoft.NeighborhoodToolCoop.Server -c Release -o ./publish
```

### Set Google OAuth Credentials (required for auth)
```bash
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID" --project src/SpearSoft.NeighborhoodToolCoop.Server
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_SECRET" --project src/SpearSoft.NeighborhoodToolCoop.Server
```

### No tests yet — MVP phase. No linting tools configured.

## Architecture

### Project Layout
- **Server** (`src/SpearSoft.NeighborhoodToolCoop.Server`) — ASP.NET Core host, Minimal API endpoints, Dapper repositories, DbUp migrations, middleware, auth, Hangfire background jobs, label/QR generation. Also serves the compiled WASM client via `UseBlazorFrameworkFiles()`.
- **Client** (`src/SpearSoft.NeighborhoodToolCoop.Client`) — Blazor WASM frontend; routable pages under `Pages/`, reusable components under `Shared/`, HTTP client services under `Services/`.
- **Shared** (`src/SpearSoft.NeighborhoodToolCoop.Shared`) — Domain models and API request/response DTOs shared between Server and Client.

### Multi-Tenancy
Single database with row-level tenant isolation. Every table has `tenant_id UUID NOT NULL`. All repositories extend `RepositoryBase`, which throws `InvalidOperationException` if `TenantContext.IsResolved == false`.

Tenant is resolved in two modes:
1. **URL-based (MVP):** `TenantResolutionMiddleware` parses `/t/{slug}/…` and queries the `tenants` table.
2. **Claim-based (authenticated API calls):** Reads `tenant_id` and `tenant_slug` from the cookie-backed auth claims.

All routes requiring tenant context must be under `/t/{tenantSlug}/…` or behind authentication.

### Authentication Flow
Google OAuth (OIDC) via `GoogleAuthEvents.OnCreatingTicket`:
1. Looks up user by `(tenant_id, google_subject)`.
2. Auto-creates user with `Pending` status if first login.
3. Adds custom claims: `app_user_id`, `tenant_id`, `tenant_slug`, `role`, `member_status`.
4. ASP.NET Core issues a `toolcoop.auth` cookie.
5. Blazor WASM bootstraps `AuthenticationState` by calling `GET /api/me` (`ServerAuthenticationStateProvider`).

### Data Access
No Entity Framework. All SQL is hand-written with Dapper. `DbConnectionFactory` (singleton) manages Npgsql connection pooling. Repositories are scoped per-request and receive `TenantContext` via DI.

Database migrations are embedded SQL scripts in `Server/Data/Migrations/` and run via DbUp at startup. `V002__seed_dev_data.sql` only runs in the Development environment.

### API Layer
Minimal APIs (no MVC controllers). Endpoints grouped in `Server/Endpoints/*.cs` and registered in `Program.cs`. Authorization policies:
- `ManagerOrAbove` — Admin or Manager role
- `AdminOnly` — Admin role only

### Key PostgreSQL Features
- `pgcrypto` extension for UUID generation (`gen_random_uuid()`)
- `ltree` extension for hierarchical storage locations (e.g., `SHED.Shelf1.BinA`)
- `set_updated_utc()` trigger maintains `updated_utc` on all tables automatically

### QR Code & Label Pipeline
- `QrCodeGenerator` (singleton) — produces PNG bytes via QRCoder
- `SvgLabelBuilder` — assembles 2×1 inch SVG labels (DYMO/Zebra compatible)
- `LabelService` (scoped) — orchestrates single SVG export or batch HTML export for printing
- QR codes encode: `https://{baseUrl}/t/{tenantSlug}/tool/{toolId}?a=scan`

### Background Jobs
Hangfire with PostgreSQL storage. Dashboard at `/hangfire`. Jobs handle reminders, overdue escalation, and notifications (partially implemented).
