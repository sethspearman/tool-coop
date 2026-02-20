-- ============================================================
-- Migration V002: Development Seed Data
-- DEVELOPMENT ONLY — DatabaseMigrator skips this script in
-- non-Development environments based on the filename convention.
-- ============================================================

-- Seed tenant
INSERT INTO tenants (id, name, slug, plan, status)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Woodruff Neighborhood Co-op',
    'woodruff',
    'Community',
    'Active'
) ON CONFLICT DO NOTHING;

-- Seed admin user (update google_subject after first Google login)
INSERT INTO users (id, tenant_id, display_name, email, google_subject)
VALUES (
    '00000000-0000-0000-0000-000000000010',
    '00000000-0000-0000-0000-000000000001',
    'Admin User',
    'admin@example.com',
    'google-subject-placeholder'
) ON CONFLICT DO NOTHING;

UPDATE tenants
   SET owner_user_id = '00000000-0000-0000-0000-000000000010'
 WHERE id = '00000000-0000-0000-0000-000000000001'
   AND owner_user_id IS NULL;

INSERT INTO tenant_users (tenant_id, user_id, role, status)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000010',
    'Admin',
    'Active'
) ON CONFLICT DO NOTHING;

-- Location tree
INSERT INTO locations (id, tenant_id, name, code, path) VALUES
    ('00000000-0000-0000-0001-000000000001','00000000-0000-0000-0000-000000000001','Storage Shed','SHED','SHED'),
    ('00000000-0000-0000-0001-000000000002','00000000-0000-0000-0000-000000000001','Shelf 1','SH1','SHED.Shelf1'),
    ('00000000-0000-0000-0001-000000000003','00000000-0000-0000-0000-000000000001','Shelf 2','SH2','SHED.Shelf2'),
    ('00000000-0000-0000-0001-000000000004','00000000-0000-0000-0000-000000000001','Bin A','BINA','SHED.Shelf1.BinA'),
    ('00000000-0000-0000-0001-000000000005','00000000-0000-0000-0000-000000000001','Bin B','BINB','SHED.Shelf1.BinB'),
    ('00000000-0000-0000-0001-000000000006','00000000-0000-0000-0000-000000000001','Garage','GAR','GAR'),
    ('00000000-0000-0000-0001-000000000007','00000000-0000-0000-0000-000000000001','Wall Hooks','HOOKS','GAR.Hooks')
ON CONFLICT DO NOTHING;

-- Sample tools
INSERT INTO tools (id, tenant_id, name, category, owner_type, condition, location_id, qr_code) VALUES
    ('00000000-0000-0000-0002-000000000001','00000000-0000-0000-0000-000000000001','DeWalt 20V Drill','Power Tools','Coop','Good','00000000-0000-0000-0001-000000000004','T-0001'),
    ('00000000-0000-0000-0002-000000000002','00000000-0000-0000-0000-000000000001','Lawn Aerator','Yard','Coop','Good','00000000-0000-0000-0001-000000000006','T-0002'),
    ('00000000-0000-0000-0002-000000000003','00000000-0000-0000-0000-000000000001','Wood Chipper','Yard','Coop','Fair','00000000-0000-0000-0001-000000000006','T-0003'),
    ('00000000-0000-0000-0002-000000000004','00000000-0000-0000-0000-000000000001','Circular Saw','Power Tools','Coop','Good','00000000-0000-0000-0001-000000000004','T-0004'),
    ('00000000-0000-0000-0002-000000000005','00000000-0000-0000-0000-000000000001','Pressure Washer','Cleaning','Coop','Good','00000000-0000-0000-0001-000000000007','T-0005')
ON CONFLICT DO NOTHING;
