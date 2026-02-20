-- ============================================================
-- Neighborhood Tool Co-op
-- PostgreSQL Schema v1
-- Stack: .NET 8 / Dapper / PostgreSQL / Google OAuth
-- PKs: UUID (gen_random_uuid())
-- Locations: ltree extension for nested paths
-- Tenant isolation: tenant_id on every table (enforced in app layer)
-- ============================================================

-- ============================================================
-- EXTENSIONS
-- ============================================================
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "ltree";      -- hierarchical location paths

-- ============================================================
-- ENUMS
-- ============================================================
CREATE TYPE user_role      AS ENUM ('Admin', 'Manager', 'Member', 'Guest');
CREATE TYPE member_status  AS ENUM ('Active', 'Pending', 'Banned');
CREATE TYPE owner_type     AS ENUM ('Member', 'Coop');
CREATE TYPE tool_condition AS ENUM ('New', 'Good', 'Fair', 'NeedsRepair');
CREATE TYPE loan_status    AS ENUM ('Reserved', 'CheckedOut', 'Returned', 'Overdue', 'Canceled');
CREATE TYPE notif_status   AS ENUM ('Pending', 'Sent', 'Failed');
CREATE TYPE tenant_plan    AS ENUM ('Community', 'Standard', 'Plus');
CREATE TYPE tenant_status  AS ENUM ('Active', 'Suspended', 'Canceled');

-- ============================================================
-- TENANTS
-- ============================================================
CREATE TABLE tenants (
    id                  UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    name                VARCHAR(120) NOT NULL,
    slug                VARCHAR(60)  NOT NULL,           -- e.g. "peachtree2"
    plan                tenant_plan  NOT NULL DEFAULT 'Community',
    status              tenant_status NOT NULL DEFAULT 'Active',
    owner_user_id       UUID         NULL,               -- FK set after Users insert
    billing_customer_id VARCHAR(120) NULL,               -- Stripe customer id (future)
    created_utc         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_utc         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_tenants_slug UNIQUE (slug)
);

-- ============================================================
-- USERS
-- Note: tenant_id scopes users to a single tenant.
-- A person joining two co-ops creates two separate accounts.
-- google_subject is the OAuth "sub" claim from Google.
-- ============================================================
CREATE TABLE users (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    display_name    VARCHAR(120) NOT NULL,
    email           VARCHAR(254) NOT NULL,
    phone           VARCHAR(30)  NULL,
    avatar_url      TEXT         NULL,
    google_subject  VARCHAR(255) NOT NULL,               -- Google OAuth "sub"
    created_utc     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_utc     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_users_email_per_tenant   UNIQUE (tenant_id, email),
    CONSTRAINT uq_users_google_per_tenant  UNIQUE (tenant_id, google_subject)
);

-- Back-fill the FK now that users table exists
ALTER TABLE tenants
    ADD CONSTRAINT fk_tenants_owner_user
    FOREIGN KEY (owner_user_id) REFERENCES users(id) ON DELETE SET NULL;

-- ============================================================
-- TENANT_USERS  (membership & roles)
-- Bridges tenants <-> users with role + status.
-- Kept separate from users so roles can change without touching
-- the core user record.
-- ============================================================
CREATE TABLE tenant_users (
    tenant_id   UUID          NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id     UUID          NOT NULL REFERENCES users(id)   ON DELETE CASCADE,
    role        user_role     NOT NULL DEFAULT 'Member',
    status      member_status NOT NULL DEFAULT 'Pending',
    joined_utc  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    PRIMARY KEY (tenant_id, user_id)
);

-- ============================================================
-- LOCATIONS
-- Uses ltree for efficient subtree queries.
-- path example: 'Garage.Shelf4.BinB'
-- ltree labels must match [A-Za-z0-9_]+ (no spaces; use underscores).
-- Store human-readable name separately.
-- ============================================================
CREATE TABLE locations (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name        VARCHAR(120) NOT NULL,                   -- "Shelf 4"  (display)
    code        VARCHAR(30)  NOT NULL,                   -- short code e.g. "S4"
    path        LTREE        NOT NULL,                   -- e.g. Garage.Shelf4.BinB
    parent_id   UUID         NULL REFERENCES locations(id) ON DELETE SET NULL,
    notes       TEXT         NULL,
    created_utc TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_locations_code_per_tenant UNIQUE (tenant_id, code)
);

-- Indexes for ltree subtree / ancestor queries
CREATE INDEX idx_locations_path_gist ON locations USING GIST (path);
CREATE INDEX idx_locations_tenant    ON locations (tenant_id);

-- ============================================================
-- TOOLS
-- ============================================================
CREATE TABLE tools (
    id               UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id        UUID           NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name             VARCHAR(120)   NOT NULL,
    description      TEXT           NULL,
    category         VARCHAR(60)    NULL,
    owner_type       owner_type     NOT NULL DEFAULT 'Coop',
    owner_user_id    UUID           NULL REFERENCES users(id) ON DELETE SET NULL,
    condition        tool_condition NOT NULL DEFAULT 'Good',
    location_id      UUID           NULL REFERENCES locations(id) ON DELETE SET NULL,
    qr_code          VARCHAR(60)    NOT NULL,             -- unique short code
    image_url        TEXT           NULL,
    value_estimate   NUMERIC(10,2)  NULL,
    deposit_required NUMERIC(10,2)  NULL DEFAULT 0,
    is_active        BOOLEAN        NOT NULL DEFAULT TRUE,
    created_utc      TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_utc      TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    created_by       UUID           NULL REFERENCES users(id) ON DELETE SET NULL,
    updated_by       UUID           NULL REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT uq_tools_qr_per_tenant UNIQUE (tenant_id, qr_code)
);

CREATE INDEX idx_tools_tenant       ON tools (tenant_id);
CREATE INDEX idx_tools_location     ON tools (location_id);
CREATE INDEX idx_tools_category     ON tools (tenant_id, category);
CREATE INDEX idx_tools_active       ON tools (tenant_id, is_active);

-- ============================================================
-- TOOL_ATTRIBUTES  (key/value extensibility)
-- e.g. key='amperage', value='20A'
-- ============================================================
CREATE TABLE tool_attributes (
    id        UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    tool_id   UUID         NOT NULL REFERENCES tools(id)   ON DELETE CASCADE,
    key       VARCHAR(60)  NOT NULL,
    value     VARCHAR(255) NOT NULL,

    CONSTRAINT uq_tool_attr UNIQUE (tool_id, key)
);

CREATE INDEX idx_tool_attributes_tool ON tool_attributes (tool_id);

-- ============================================================
-- LOANS
-- ============================================================
CREATE TABLE loans (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    tool_id      UUID        NOT NULL REFERENCES tools(id)   ON DELETE RESTRICT,
    borrower_id  UUID        NOT NULL REFERENCES users(id)   ON DELETE RESTRICT,
    start_utc    TIMESTAMPTZ NOT NULL,
    due_utc      TIMESTAMPTZ NOT NULL,
    returned_utc TIMESTAMPTZ NULL,
    status       loan_status NOT NULL DEFAULT 'Reserved',
    notes        TEXT        NULL,
    created_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by   UUID        NULL REFERENCES users(id) ON DELETE SET NULL,
    updated_by   UUID        NULL REFERENCES users(id) ON DELETE SET NULL
);

CREATE INDEX idx_loans_tenant      ON loans (tenant_id);
CREATE INDEX idx_loans_tool        ON loans (tool_id);
CREATE INDEX idx_loans_borrower    ON loans (borrower_id);
CREATE INDEX idx_loans_status      ON loans (tenant_id, status);
CREATE INDEX idx_loans_due         ON loans (tenant_id, due_utc) WHERE returned_utc IS NULL;

-- ============================================================
-- RESERVATIONS
-- ============================================================
CREATE TABLE reservations (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    tool_id      UUID        NOT NULL REFERENCES tools(id)   ON DELETE CASCADE,
    user_id      UUID        NOT NULL REFERENCES users(id)   ON DELETE CASCADE,
    window_start TIMESTAMPTZ NOT NULL,
    window_end   TIMESTAMPTZ NOT NULL,
    status       VARCHAR(30) NOT NULL DEFAULT 'Pending',     -- Pending/Approved/Canceled
    created_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_reservation_window CHECK (window_end > window_start)
);

CREATE INDEX idx_reservations_tenant ON reservations (tenant_id);
CREATE INDEX idx_reservations_tool   ON reservations (tool_id, window_start, window_end);

-- ============================================================
-- INCIDENTS  (damage / loss reports)
-- ============================================================
CREATE TABLE incidents (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    loan_id     UUID        NOT NULL REFERENCES loans(id)   ON DELETE RESTRICT,
    tool_id     UUID        NOT NULL REFERENCES tools(id)   ON DELETE RESTRICT,
    reported_by UUID        NOT NULL REFERENCES users(id)   ON DELETE RESTRICT,
    description TEXT        NOT NULL,
    photo_urls  TEXT[]      NULL,                            -- array of image URLs
    resolved    BOOLEAN     NOT NULL DEFAULT FALSE,
    resolved_by UUID        NULL REFERENCES users(id) ON DELETE SET NULL,
    resolved_utc TIMESTAMPTZ NULL,
    created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_incidents_tenant ON incidents (tenant_id);
CREATE INDEX idx_incidents_tool   ON incidents (tool_id);

-- ============================================================
-- NOTIFICATIONS
-- ============================================================
CREATE TABLE notifications (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id     UUID         NOT NULL REFERENCES users(id)   ON DELETE CASCADE,
    type        VARCHAR(60)  NOT NULL,                       -- e.g. 'LoanDueSoon', 'Overdue'
    payload     JSONB        NULL,
    status      notif_status NOT NULL DEFAULT 'Pending',
    sent_utc    TIMESTAMPTZ  NULL,
    created_utc TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user   ON notifications (tenant_id, user_id);
CREATE INDEX idx_notifications_status ON notifications (status) WHERE status = 'Pending';

-- ============================================================
-- AUDIT_LOG  (append-only; no updates/deletes)
-- ============================================================
CREATE TABLE audit_log (
    id           BIGSERIAL    PRIMARY KEY,                   -- sequential for ordering
    tenant_id    UUID         NOT NULL,                      -- no FK; log survives tenant delete
    actor_id     UUID         NULL,                          -- user who did the action
    action       VARCHAR(60)  NOT NULL,                      -- e.g. 'Tool.Create', 'Loan.Checkout'
    entity_type  VARCHAR(60)  NOT NULL,                      -- 'Tool', 'Loan', etc.
    entity_id    UUID         NULL,
    old_values   JSONB        NULL,
    new_values   JSONB        NULL,
    ip_address   INET         NULL,
    created_utc  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_tenant  ON audit_log (tenant_id, created_utc DESC);
CREATE INDEX idx_audit_entity  ON audit_log (entity_type, entity_id);

-- ============================================================
-- UPDATED_UTC TRIGGER FUNCTION
-- Automatically updates updated_utc on any row change.
-- ============================================================
CREATE OR REPLACE FUNCTION set_updated_utc()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_utc = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to all tables with updated_utc
DO $$
DECLARE
    t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'tenants','users','tenant_users','locations',
        'tools','loans','reservations','incidents'
    ] LOOP
        EXECUTE format(
            'CREATE TRIGGER trg_%I_updated_utc
             BEFORE UPDATE ON %I
             FOR EACH ROW EXECUTE FUNCTION set_updated_utc()',
            t, t
        );
    END LOOP;
END;
$$;

-- ============================================================
-- SEED DATA (development / local testing)
-- ============================================================

-- Tenant
INSERT INTO tenants (id, name, slug, plan, status)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Woodruff Neighborhood Co-op',
    'woodruff',
    'Community',
    'Active'
);

-- Admin user (you — update google_subject after first Google login)
INSERT INTO users (id, tenant_id, display_name, email, google_subject)
VALUES (
    '00000000-0000-0000-0000-000000000010',
    '00000000-0000-0000-0000-000000000001',
    'Admin User',
    'admin@example.com',
    'google-subject-placeholder'
);

UPDATE tenants
SET owner_user_id = '00000000-0000-0000-0000-000000000010'
WHERE id = '00000000-0000-0000-0000-000000000001';

INSERT INTO tenant_users (tenant_id, user_id, role, status)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000010',
    'Admin',
    'Active'
);

-- Location tree (path uses ltree dot-notation; no spaces)
INSERT INTO locations (id, tenant_id, name, code, path)
VALUES
    ('00000000-0000-0000-0001-000000000001', '00000000-0000-0000-0000-000000000001', 'Storage Shed',  'SHED',    'SHED'),
    ('00000000-0000-0000-0001-000000000002', '00000000-0000-0000-0000-000000000001', 'Shelf 1',       'SH1',     'SHED.Shelf1'),
    ('00000000-0000-0000-0001-000000000003', '00000000-0000-0000-0000-000000000001', 'Shelf 2',       'SH2',     'SHED.Shelf2'),
    ('00000000-0000-0000-0001-000000000004', '00000000-0000-0000-0000-000000000001', 'Bin A',         'BINA',    'SHED.Shelf1.BinA'),
    ('00000000-0000-0000-0001-000000000005', '00000000-0000-0000-0000-000000000001', 'Bin B',         'BINB',    'SHED.Shelf1.BinB'),
    ('00000000-0000-0000-0001-000000000006', '00000000-0000-0000-0000-000000000001', 'Garage',        'GAR',     'GAR'),
    ('00000000-0000-0000-0001-000000000007', '00000000-0000-0000-0000-000000000001', 'Wall Hooks',    'HOOKS',   'GAR.Hooks');

-- Sample tools
INSERT INTO tools (id, tenant_id, name, category, owner_type, condition, location_id, qr_code)
VALUES
    ('00000000-0000-0000-0002-000000000001', '00000000-0000-0000-0000-000000000001',
     'DeWalt 20V Drill',      'Power Tools', 'Coop', 'Good', '00000000-0000-0000-0001-000000000004', 'T-0001'),
    ('00000000-0000-0000-0002-000000000002', '00000000-0000-0000-0000-000000000001',
     'Lawn Aerator',          'Yard',        'Coop', 'Good', '00000000-0000-0000-0001-000000000006', 'T-0002'),
    ('00000000-0000-0000-0002-000000000003', '00000000-0000-0000-0000-000000000001',
     'Wood Chipper',          'Yard',        'Coop', 'Fair', '00000000-0000-0000-0001-000000000006', 'T-0003'),
    ('00000000-0000-0000-0002-000000000004', '00000000-0000-0000-0000-000000000001',
     'Circular Saw',          'Power Tools', 'Coop', 'Good', '00000000-0000-0000-0001-000000000004', 'T-0004'),
    ('00000000-0000-0000-0002-000000000005', '00000000-0000-0000-0000-000000000001',
     'Pressure Washer',       'Cleaning',    'Coop', 'Good', '00000000-0000-0000-0001-000000000007', 'T-0005');

-- ============================================================
-- USEFUL QUERIES (reference / Dapper examples)
-- ============================================================

-- All tools in a subtree (e.g. everything in the Shed):
-- SELECT t.* FROM tools t
-- JOIN locations l ON l.id = t.location_id
-- WHERE t.tenant_id = @tenantId
--   AND l.path <@ 'SHED';          -- <@ means "is descendant of"

-- Depth of a location:
-- SELECT nlevel(path) AS depth FROM locations WHERE id = @locationId;

-- Currently checked-out tools (no returned_utc):
-- SELECT t.name, u.display_name, l.due_utc
-- FROM loans l
-- JOIN tools t ON t.id = l.tool_id
-- JOIN users u ON u.id = l.borrower_id
-- WHERE l.tenant_id = @tenantId
--   AND l.status = 'CheckedOut';

-- Overdue loans (for Hangfire nightly job):
-- SELECT * FROM loans
-- WHERE tenant_id = @tenantId
--   AND status = 'CheckedOut'
--   AND due_utc < NOW();
