# Neighborhood Tool Co‑op – Multi‑Tenant Design Spec (v2)

_Original: 2025‑09‑24 | Updated: 2026‑02‑19_

> **v2 Change Summary:** Finalized tech stack (PostgreSQL + Dapper, Blazor WASM hosted, Google OAuth only). Switched PKs from ULIDs to GUIDs. Replaced `hierarchyid` with PostgreSQL `ltree`. Tenant resolution changed to URL prefix for MVP. Tenant identity model clarified (one login per tenant — cross-tenant users create separate accounts). QR scan strategy simplified (native camera, no JS interop). Hosting changed to home Ubuntu 22.04/24.04 Linux server for MVP phase. Deliverables updated to reflect completed schema.

---

## 1) Goal & Scope
Build a reusable, multi‑tenant platform that lets neighborhoods, HOAs, churches, or teams manage a shared inventory of tools with check‑in/out, reservations, and location tracking. SaaS‑ready from day one.

### Personas
- **Member** – browses, reserves, borrows, returns tools.
- **Tool Owner** – lists tools, sets availability/terms.
- **Coordinator/Manager** – approves members, manages inventory, resolves issues.
- **Tenant Admin** – billing, settings, roles, custom domain.

---

## 2) Tenancy Model
- **Strategy:** Single app + single database, **row‑level tenant isolation** via `tenant_id` (UUID/GUID) on every table.
- **Tenant resolution (MVP):** URL prefix `/t/{tenantSlug}`. Simpler for Blazor WASM since the app runs entirely in the browser and does not have server-side middleware naturally intercepting host headers.
- **Tenant resolution (v2):** Host header (e.g., `peachtree2.toolcoop.app`) with optional custom domains via Nginx map or Cloudflare Worker.
- **User identity:** Users are scoped per tenant. A person participating in two co‑ops creates two separate accounts. There is no cross‑tenant identity. This simplifies the data model and avoids "which tenant am I acting as" UI complexity.
- **Advantages:** Lowest ops cost, simple migrations, shared pools. (Alternative: per‑tenant schema is heavier; reserve only if compliance demands DB‑per‑tenant.)

---

## 3) Tech Stack (finalized)
- **Backend:** .NET 8 WebAPI (C#)
- **ORM:** Dapper (lightweight; full SQL control; no EF Core)
- **DB:** PostgreSQL (latest stable) with `ltree` extension for nested locations
- **PKs:** UUID (`gen_random_uuid()`) on all tables
- **Auth:** Google OAuth only via OpenID Connect (MVP). Microsoft OAuth and magic‑link passwordless deferred to v2.
- **Frontend:** Blazor WebAssembly — ASP.NET Core Hosted model (single deployable unit; server project serves the WASM client)
- **Background jobs:** Hangfire (PostgreSQL storage) for reminders & nightly overdue checks
- **Payments:** Stripe (Subscriptions + Billing portal) — post-MVP
- **Email/SMS:** Postmark (email) + Twilio (SMS) — pluggable providers
- **Hosting (MVP):** Home Ubuntu 22.04/24.04 Linux server; Nginx reverse proxy; systemd service; Cloudflare free tier for HTTPS and DNS (hides home IP, handles TLS)
- **Hosting (production):** Migrate to Linode or similar VPS when commercially viable

---

## 4) Data Model (MVP)

All tables include: `tenant_id UUID NOT NULL`, `created_utc TIMESTAMPTZ`, `updated_utc TIMESTAMPTZ` (auto‑maintained via trigger), `created_by UUID NULL`, `updated_by UUID NULL`.

- **tenants**: `id UUID PK`, `name`, `slug (unique)`, `plan ENUM(Community|Standard|Plus)`, `status ENUM(Active|Suspended|Canceled)`, `owner_user_id FK`, `billing_customer_id` (Stripe, nullable).
- **users**: `id UUID PK`, `tenant_id FK`, `display_name`, `email (unique per tenant)`, `phone`, `avatar_url`, `google_subject (unique per tenant)` — the Google OAuth `sub` claim used for login matching.
- **tenant_users** (membership & roles): `tenant_id + user_id (composite PK)`, `role ENUM(Admin|Manager|Member|Guest)`, `status ENUM(Active|Pending|Banned)`.
- **locations**: `id UUID PK`, `tenant_id FK`, `name` (display), `code (unique per tenant)`, `path LTREE`, `parent_id FK NULL`, `notes`.
- **tools**: `id UUID PK`, `tenant_id FK`, `name`, `description`, `category`, `owner_type ENUM(Member|Coop)`, `owner_user_id FK NULL`, `condition ENUM(New|Good|Fair|NeedsRepair)`, `location_id FK NULL`, `qr_code (unique per tenant)`, `image_url`, `value_estimate`, `deposit_required`, `is_active`.
- **tool_attributes**: key/value extensibility per tool (e.g., amperage, blade size, battery type).
- **loans**: `id UUID PK`, `tenant_id FK`, `tool_id FK`, `borrower_id FK`, `start_utc`, `due_utc`, `returned_utc NULL`, `status ENUM(Reserved|CheckedOut|Returned|Overdue|Canceled)`, `notes`.
- **reservations**: `id UUID PK`, `tenant_id FK`, `tool_id FK`, `user_id FK`, `window_start`, `window_end`, `status`. Includes check constraint: `window_end > window_start`.
- **incidents**: damage/loss reports; `photo_urls TEXT[]` (PostgreSQL array); resolved flag + resolver FK.
- **notifications**: `id UUID PK`, `tenant_id FK`, `user_id FK`, `type`, `payload JSONB`, `status ENUM(Pending|Sent|Failed)`, `sent_utc`.
- **audit_log**: `id BIGSERIAL PK` (sequential for ordering), append‑only, no FK to tenants (log survives tenant deletion). Stores `old_values` and `new_values` as JSONB.

### Location Tree with `ltree` (PostgreSQL)
- Store `locations.path` as `ltree`. Labels use `[A-Za-z0-9_]+` dot‑separated notation (no spaces).
- Sample paths: `SHED`, `SHED.Shelf1`, `SHED.Shelf1.BinA`, `GAR.Hooks`.
- The `name` column holds the human‑readable display value (e.g., "Shelf 1").
- Indexed with `GIST` for efficient subtree queries.
- Key queries: subtree (`path <@ 'SHED'`), depth (`nlevel(path)`), children (`path ~ 'SHED.*{1}'`).

### updated_utc Trigger
A single `set_updated_utc()` PL/pgSQL function is applied as a `BEFORE UPDATE` trigger to all tables with an `updated_utc` column, ensuring it is always maintained automatically.

---

## 5) QR/Barcode Scheme
- **Label contents (human‑readable + QR)**:
  - Line 1: Tool name (short)
  - Line 2: Tenant slug + short tool code (e.g., `woodruff ▸ T-0001`)
  - QR payload: `https://toolcoop.yourdomain.com/t/{tenantSlug}/tool/{toolId}?a=scan`
- **On scan:** User scans with their phone's **native camera app** (iOS and Android both decode QR natively — no JS camera interop needed in Blazor). The QR URL opens directly in the mobile browser. If tool is available, offer **Check Out**; if checked‑out to the scanning user, offer **Return**.
- **Checksum:** Optional short checksum in code to prevent fat‑finger entry.
- **Label size:** 2"×1" thermal (DYMO/Zebra). Include current **Location Code** on label; print a small **Location label** for shelves/bins too.

---

## 6) Permissions & Policies
- **Roles**
  - **Admin**: tenant settings, billing, roles, all inventory.
  - **Manager**: manage tools/loans/locations; approve members.
  - **Member**: borrow/return, list personal tools, view schedules.
  - **Guest (read‑only)**: browse catalog if tenant opts in.
- **Ownership**
  - **Co‑op owned** tools: all Members may request; Managers approve auto/manual.
  - **Member owned** tools: owner can set who may borrow (All Members / Trusted circle / Approval required).
- **Limits** (plan‑based): max active loans per user, loan duration caps, # tools, storage quota, SMS credits.

---

## 7) Core Workflows

### 7.1 Onboarding a New Tenant
1. Admin signs up → creates Tenant (slug auto‑generated).
2. Configure: name/logo, locale/timezone, pickup/return rules, default loan length, deposit rules.
3. Invite members (email link) or open join with approval.
4. Create Location tree (bulk import or UI).
5. Add tools (CSV import or one‑by‑one), print labels.

### 7.2 Borrow → Return
- Member finds tool → **Reserve** (optional) → **Check Out** via QR or UI.
- Due reminders: T‑24h, T‑0h; **Overdue** escalation to Manager (Hangfire nightly job).
- Return via QR at return station; prompt for condition + photos.
- If damage reported → auto‑create Incident → Manager workflow.

### 7.3 Nested Locations (Setup & Move)
- Drag‑and‑drop tree UI; bulk move subtree (ltree path update).
- Scan **Location label** → "Set current location" → Scan tool(s) to batch move.
- Audit every move via audit_log.

### 7.4 Google OAuth Login Flow
1. User hits `/t/{tenantSlug}` → redirected to Google sign‑in.
2. On return, API receives Google `sub` claim.
3. Look up `users` by `(tenant_id, google_subject)`.
4. If found → issue app session. If not found → create new user record (status: Pending) → await Manager approval or auto-approve based on tenant policy.

---

## 8) APIs (sample, REST)
Base path: `/api/v1`. All calls require tenant context resolved from URL prefix (`/t/{tenantSlug}`).

- `POST /tenants` – create tenant (platform admin only)
- `GET /me` – current user + tenant role
- `GET /locations?parentId=` – list children; `POST /locations` create; `PATCH /locations/{id}` move/rename
- `GET /tools` (filters: q, category, availability, locationPath)
- `POST /tools` create; `PATCH /tools/{id}` update
- `POST /tools/{id}/labels` – returns PNG/SVG for printing
- `POST /loans` (toolId, start, due)
- `POST /loans/{id}/checkout` / `POST /loans/{id}/return`
- `POST /reservations`
- `GET /notifications`

---

## 9) UI Sketch (high‑level)
- **Dashboard:** My Loans, Due Soon, Requests, Quick Scan.
- **Catalog:** filter by category, availability, owner type. (Distance filter deferred.)
- **Tool Detail:** photos, specs, condition, location path (ltree breadcrumb), availability calendar, actions.
- **Locations:** tree view + QR print helper.
- **Admin:** members & roles, policies, billing, plan limits, webhooks.

---

## 10) Billing & Plans (draft)
- **Free (Community):** up to 25 members, 150 tools, 200 SMS/mo, core features.
- **Standard ($9/mo):** 100 members, 1,000 tools, reservations, incidents, export.
- **Plus ($19/mo):** 300 members, 3,000 tools, custom domain, SSO, advanced reports.
- **Add‑ons:** extra SMS, storage, premium support, weekend equipment insurance (partner).

---

## 11) Ops, Security, & Compliance
- **Isolation:** `tenant_id` enforced in all Dapper queries (parameterized); no global EF query filter — discipline enforced via repository pattern and code review.
- **AuthZ:** Policy‑based + resource ownership checks in API layer.
- **PII:** Minimal; TLS in transit (Cloudflare + Nginx); PostgreSQL encryption at rest optional.
- **Backups:** pg_dump nightly; WAL archiving for point‑in‑time restore (configure once production traffic warrants).
- **Monitoring:** Structured logs (Serilog → file/Seq); health check endpoint; alerting on Hangfire job failures.
- **Rate limiting** & anti‑abuse; CAPTCHA on public join flows.
- **audit_log** for key actions (tool create/update, loan checkout/return, role changes, location moves).

---

## 12) Label Printing Pipeline
- Generate **SVG/PNG** server‑side; batch export to PDF for 2×1 label sheets.
- Include QR + short tool code + current location code.
- Optional **WebUSB**/print helper for Zebra/ZPL printers (future).

---

## 13) MVP Backlog (6–8 weeks part‑time)
1. ~~SQL Server schema~~ → **PostgreSQL schema with `ltree` + seed data** ✅
2. .NET 8 solution structure (WebAPI + Blazor WASM hosted + shared contracts library).
3. Google OAuth wiring (OIDC in Blazor WASM + API token validation).
4. Dapper repository layer with tenant context middleware.
5. Locations: CRUD + ltree tree UI.
6. Tools: CRUD, photos, attributes, label generator.
7. Loans: basic checkout/return + Hangfire reminders.
8. QR scan flows (mobile web via native camera).
9. Email notifications; minimal SMS.
10. Admin settings; basic audit log.

**Stretch**: Reservations calendar, incidents, exports, member reputations, custom domain support.

---

## 14) Data Contracts (abbrev)
```json
// Tool (read)
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "DeWalt 20V Drill",
  "tenantId": "00000000-0000-0000-0000-000000000001",
  "category": "Power Tools",
  "condition": "Good",
  "ownerType": "Coop",
  "ownerUserId": null,
  "location": {
    "id": "00000000-0000-0000-0001-000000000004",
    "path": "SHED.Shelf1.BinA",
    "displayPath": "Storage Shed / Shelf 1 / Bin A"
  },
  "availability": { "status": "Available", "nextReservation": null },
  "qrUrl": "https://toolcoop.yourdomain.com/t/woodruff/tool/3fa85f64-5717-4562-b3fc-2c963f66afa6?a=scan"
}
```

---

## 15) Risk & Mitigations
- **Lost/damaged tools:** deposits, incident workflow, optional insurance partner.
- **Low adoption:** seed with a few high‑value co‑op tools (aerator, chipper) and do seasonal events.
- **Member reliability:** soft reputation; strike policy; coordinator approvals.
- **Home server availability:** acceptable for MVP/dev; migrate to VPS before any paying tenants.
- **Concurrent writes (PostgreSQL):** Proper connection pooling (Npgsql); advisory locks or optimistic concurrency for loan checkout to prevent double-booking.

---

## 16) Future Ideas
- **Kiosks/lockers** with smart locks; geofence returns.
- **Bulk import from spreadsheets**; photo capture via PWA camera.
- **Public catalog** share link for recruiting new members.
- **Open API** for hobbyist automations; Home Assistant integration.
- **Custom domain support** (host-header tenant resolution via Nginx map or Cloudflare Worker).
- **Microsoft OAuth** as a second provider option.

---

## Deliverables Status

| # | Deliverable | Status |
|---|-------------|--------|
| 1 | PostgreSQL schema (`ltree`) + seed data | ✅ Complete (`toolcoop_schema_v1.sql`) |
| 2 | .NET 8 solution structure | 🔲 Next |
| 3 | Google OAuth (OIDC) wiring | 🔲 Pending |
| 4 | Dapper repository layer + tenant middleware | 🔲 Pending |
| 5 | Label generator service (SVG → PDF) + QR spec | 🔲 Pending |
| 6 | Blazor WASM frontend shell with QR scan & checkout | 🔲 Pending |
