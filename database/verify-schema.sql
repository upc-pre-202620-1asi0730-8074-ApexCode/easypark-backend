-- Run from MySQL Workbench with the easypark database selected.
SELECT table_name FROM information_schema.tables
WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE'
ORDER BY table_name;

-- Expect 15 business tables plus __EFMigrationsHistory (technical table).
SELECT COUNT(*) AS business_tables
FROM information_schema.tables
WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE'
AND table_name <> '__EFMigrationsHistory';

SELECT migration_id, product_version FROM `__EFMigrationsHistory`
ORDER BY migration_id;
