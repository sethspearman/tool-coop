-- ============================================================
-- Migration V001: Initial Schema
-- Applied by DbUp on startup. Idempotent guard: DbUp only runs
-- this script once (tracked in the schemaversions table).
-- ============================================================

-- Extensions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "ltree";

-- Enums
CREATE TYPE user_role      AS ENUM ('Admin', 'Manager', 'Member', 'Guest');
CREATE TYPE member_status  AS ENUM ('Active', 'Pending', 'Banned');
CREATE TYPE owner_type     AS ENUM ('Member', 'Coop');
CREATE TYPE tool_condition AS ENUM ('New', 'Good', 'Fair', 'NeedsRepair');
CREATE TYPE loan_status    AS ENUM ('Reserved', 'CheckedOut', 'Returned', 'Overdue', 'Canceled');
CREATE TYPE notif_status   AS ENUM ('Pending', 'Sent', 'Failed');
CREATE TYPE tenant_plan    AS ENUM ('Community', 'Standard', 'Plus');
CREATE TYPE tenant_status  AS ENUM ('Active', 'Suspended', 'Canceled');

-- Tenants
CREATE TABLE tenants (
    id                  UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    name                VARCHAR(120)  NOT NULL,
    slug                VARCHAR(60)   NOT NULL,
    plan                tenant_plan   NOT NULL DEFAULT 'Community',
    status              tenant_status NOT NULL DEFAULT 'Active',
    owner_user_id       UUID          NULL,
    billing_customer_id VARCHAR(120)  NULL,
    created_utc         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_utc         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_tenants_slug UNIQUE (slug)
);

-- Users
CREATE TABLE users (
    id             UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id      UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    display_name   VARCHAR(120) NOT NULL,
    email          VARCHAR(254) NOT NULL,
    phone          VARCHAR(30)  NULL,
    avatar_url     TEXT         NULL,
    google_subject VARCHAR(255) NOT NULL,
    created_utc    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_utc    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_users_email_per_tenant  UNIQUE (tenant_id, email),
    CONSTRAINT uq_users_google_per_tenant UNIQUE (tenant_id, google_subject)
);

ALTER TABLE tenants
    ADD CONSTRAINT fk_tenants_owner_user
    FOREIGN KEY (owner_user_id) REFERENCES users(id) ON DELETE SET NULL;

-- Tenant membership & roles
CREATE TABLE tenant_users (
    tenant_id   UUID          NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id     UUID          NOT NULL REFERENCES users(id)   ON DELETE CASCADE,
    role        user_role     NOT NULL DEFAULT 'Member',
    status      member_status NOT NULL DEFAULT 'Pending',
    joined_utc  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    PRIMARY KEY (tenant_id, user_id)
);

-- Locations (ltree)
CREATE TABLE locations (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name        VARCHAR(120) NOT NULL,
    code        VARCHAR(30)  NOT NULL,
    path        LTREE        NOT NULL,
    parent_id   UUID         NULL REFERENCES locations(id) ON DELETE SET NULL,
    notes       TEXT         NULL,
    created_utc TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_locations_code_per_tenant UNIQUE (tenant_id, code)
);
CREATE INDEX idx_locations_path_gist ON locations USING GIST (path);
CREATE INDEX idx_locations_tenant    ON locations (tenant_id);

-- Tools
CREATE TABLE tools (
    id               UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id        UUID           NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name             VARCHAR(120)   NOT NULL,
    description      TEXT           NULL,
    category         VARCHAR(60)    NULL,
    owner_type       owner_type     NOT NULL DEFAULT 'Coop',
    owner_user_id    UUID           NULL REFERENCES users(id)     ON DELETE SET NULL,
    condition        tool_condition NOT NULL DEFAULT 'Good',
    location_id      UUID           NULL REFERENCES locations(id) ON DELETE SET NULL,
    qr_code          VARCHAR(60)    NOT NULL,
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
CREATE INDEX idx_tools_tenant   ON tools (tenant_id);
CREATE INDEX idx_tools_location ON tools (location_id);
CREATE INDEX idx_tools_category ON tools (tenant_id, category);
CREATE INDEX idx_tools_active   ON tools (tenant_id, is_active);

-- Tool attributes (key/value extensibility)
CREATE TABLE tool_attributes (
    id        UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    tool_id   UUID         NOT NULL REFERENCES tools(id)   ON DELETE CASCADE,
    key       VARCHAR(60)  NOT NULL,
    value     VARCHAR(255) NOT NULL,
    CONSTRAINT uq_tool_attr UNIQUE (tool_id, key)
);
CREATE INDEX idx_tool_attributes_tool ON tool_attributes (tool_id);

-- Loans
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
CREATE INDEX idx_loans_tenant   ON loans (tenant_id);
CREATE INDEX idx_loans_tool     ON loans (tool_id);
CREATE INDEX idx_loans_borrower ON loans (borrower_id);
CREATE INDEX idx_loans_status   ON loans (tenant_id, status);
CREATE INDEX idx_loans_due      ON loans (tenant_id, due_utc) WHERE returned_utc IS NULL;

-- Reservations
CREATE TABLE reservations (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    tool_id      UUID        NOT NULL REFERENCES tools(id)   ON DELETE CASCADE,
    user_id      UUID        NOT NULL REFERENCES users(id)   ON DELETE CASCADE,
    window_start TIMESTAMPTZ NOT NULL,
    window_end   TIMESTAMPTZ NOT NULL,
    status       VARCHAR(30) NOT NULL DEFAULT 'Pending',
    created_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_reservation_window CHECK (window_end > window_start)
);
CREATE INDEX idx_reservations_tenant ON reservations (tenant_id);
CREATE INDEX idx_reservations_tool   ON reservations (tool_id, window_start, window_end);

-- Incidents
CREATE TABLE incidents (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    loan_id      UUID        NOT NULL REFERENCES loans(id)   ON DELETE RESTRICT,
    tool_id      UUID        NOT NULL REFERENCES tools(id)   ON DELETE RESTRICT,
    reported_by  UUID        NOT NULL REFERENCES users(id)   ON DELETE RESTRICT,
    description  TEXT        NOT NULL,
    photo_urls   TEXT[]      NULL,
    resolved     BOOLEAN     NOT NULL DEFAULT FALSE,
    resolved_by  UUID        NULL REFERENCES users(id) ON DELETE SET NULL,
    resolved_utc TIMESTAMPTZ NULL,
    created_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_incidents_tenant ON incidents (tenant_id);
CREATE INDEX idx_incidents_tool   ON incidents (tool_id);

-- Notifications
CREATE TABLE notifications (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id     UUID         NOT NULL REFERENCES users(id)   ON DELETE CASCADE,
    type        VARCHAR(60)  NOT NULL,
    payload     JSONB        NULL,
    status      notif_status NOT NULL DEFAULT 'Pending',
    sent_utc    TIMESTAMPTZ  NULL,
    created_utc TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_notifications_user   ON notifications (tenant_id, user_id);
CREATE INDEX idx_notifications_status ON notifications (status) WHERE status = 'Pending';

-- Audit log (append-only)
CREATE TABLE audit_log (
    id          BIGSERIAL    PRIMARY KEY,
    tenant_id   UUID         NOT NULL,
    actor_id    UUID         NULL,
    action      VARCHAR(60)  NOT NULL,
    entity_type VARCHAR(60)  NOT NULL,
    entity_id   UUID         NULL,
    old_values  JSONB        NULL,
    new_values  JSONB        NULL,
    ip_address  INET         NULL,
    created_utc TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_audit_tenant ON audit_log (tenant_id, created_utc DESC);
CREATE INDEX idx_audit_entity ON audit_log (entity_type, entity_id);

-- Auto-update trigger for updated_utc
CREATE OR REPLACE FUNCTION set_updated_utc()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_utc = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
DECLARE t TEXT;
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
