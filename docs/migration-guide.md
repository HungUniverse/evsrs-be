# Migration Guide: Demand Forecast & Capacity Planning

## Overview
This migration adds support for demand forecasting and capacity planning features to the EVSRS system.

## What's Added
- **3 new tables:** `InventorySnapshot`, `DemandForecast`, `RebalancingPlan`
- **1 materialized view:** `vw_rental_demand_30m_last_56d`
- **Indexes** for performance optimization
- **Feature flags** in SystemConfig
- **Optional TimescaleDB** support for time-series data

---

## Prerequisites

1. **PostgreSQL 12+** installed and running
2. **Database backup** created (recommended)
3. **Connection string** configured in `appsettings.json`
4. **(Optional)** TimescaleDB extension for better time-series performance

---

## Migration Methods

### Option 1: Run SQL Script Directly (Recommended for Production)

```bash
# Navigate to SQL migrations folder
cd EVSRS.BusinessObjects/Migrations/SQL

# Run migration script
psql -U your_db_user -d evsrs_db -f 001_create_demand_forecast_tables.sql

# Verify tables were created
psql -U your_db_user -d evsrs_db -c "\dt *Inventory* *Demand* *Rebalancing*"

# Verify materialized view
psql -U your_db_user -d evsrs_db -c "\d+ vw_rental_demand_30m_last_56d"
```

### Option 2: Use EF Core Migration (Development)

```bash
# Navigate to API project
cd EVSRS.API

# Create new migration
dotnet ef migrations add AddDemandForecastingTables --project ../EVSRS.BusinessObjects --startup-project .

# Review generated migration in EVSRS.BusinessObjects/Migrations/

# Apply migration
dotnet ef database update --project ../EVSRS.BusinessObjects --startup-project .
```

**Note:** EF Core migrations may not create the materialized view automatically. You'll need to run the SQL script manually after EF migration:

```bash
psql -U your_db_user -d evsrs_db -f ../EVSRS.BusinessObjects/Migrations/SQL/001_create_demand_forecast_tables.sql
```

---

## Post-Migration Steps

### 1. Verify Tables and Indexes

```sql
-- Check tables exist
SELECT tablename FROM pg_tables 
WHERE tablename IN ('InventorySnapshot', 'DemandForecast', 'RebalancingPlan');

-- Check indexes
SELECT indexname FROM pg_indexes 
WHERE tablename IN ('InventorySnapshot', 'DemandForecast', 'RebalancingPlan', 'OrderBooking');

-- Check materialized view
SELECT COUNT(*) FROM vw_rental_demand_30m_last_56d;
```

### 2. Initial Data Refresh

```bash
# Refresh materialized view to populate with historical data
psql -U your_db_user -d evsrs_db -f refresh_demand_view.sql
```

### 3. Enable Feature Flags (Optional)

```sql
-- Enable forecasting feature
UPDATE "SystemConfig" 
SET "Value" = 'true' 
WHERE "Key" = 'FORECASTING_ENABLED';

-- Enable capacity advice API
UPDATE "SystemConfig" 
SET "Value" = 'true' 
WHERE "Key" = 'CAPACITY_ADVICE_ENABLED';
```

### 4. Schedule Materialized View Refresh

**Option A: Using cron (Linux/Mac)**

```bash
# Edit crontab
crontab -e

# Add line to refresh every 6 hours
0 */6 * * * psql -U your_db_user -d evsrs_db -f /path/to/refresh_demand_view.sql >> /var/log/refresh_demand.log 2>&1
```

**Option B: Using pg_cron extension**

```sql
-- Enable extension (run as superuser)
CREATE EXTENSION IF NOT EXISTS pg_cron;

-- Schedule job
SELECT cron.schedule(
    'refresh-demand-view',
    '0 */6 * * *',  -- Every 6 hours
    $$REFRESH MATERIALIZED VIEW CONCURRENTLY "vw_rental_demand_30m_last_56d"$$
);

-- Verify job scheduled
SELECT * FROM cron.job WHERE jobname = 'refresh-demand-view';
```

**Option C: Using Windows Task Scheduler**

1. Create batch file `refresh_demand.bat`:
```batch
@echo off
psql -U your_db_user -d evsrs_db -f "C:\path\to\refresh_demand_view.sql" >> "C:\logs\refresh_demand.log" 2>&1
```

2. Open Task Scheduler → Create Basic Task
3. Set trigger: Daily, repeat every 6 hours
4. Action: Start program → `refresh_demand.bat`

---

## Optional: Enable TimescaleDB

TimescaleDB provides better performance for time-series data (InventorySnapshot, DemandForecast).

### Installation

```bash
# Ubuntu/Debian
sudo apt install timescaledb-2-postgresql-15

# Or using Docker
docker run -d --name timescaledb -p 5432:5432 -e POSTGRES_PASSWORD=password timescale/timescaledb:latest-pg15
```

### Enable in Database

```sql
-- Enable extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Convert tables to hypertables
SELECT create_hypertable(
    '"InventorySnapshot"',
    '"SnapshotTime"',
    if_not_exists => TRUE,
    chunk_time_interval => INTERVAL '7 days'
);

SELECT create_hypertable(
    '"DemandForecast"',
    '"ForecastTime"',
    if_not_exists => TRUE,
    chunk_time_interval => INTERVAL '7 days'
);

-- Add compression (saves disk space)
ALTER TABLE "InventorySnapshot" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'DepotId,ModelId'
);

SELECT add_compression_policy(
    '"InventorySnapshot"',
    INTERVAL '30 days',
    if_not_exists => TRUE
);

-- Add retention policy (auto-delete old data)
SELECT add_retention_policy(
    '"InventorySnapshot"',
    INTERVAL '90 days',
    if_not_exists => TRUE
);
```

---

## Performance Tuning

### Analyze and Vacuum

```sql
-- After initial data load
ANALYZE "InventorySnapshot";
ANALYZE "DemandForecast";
ANALYZE "RebalancingPlan";
ANALYZE "vw_rental_demand_30m_last_56d";

-- Vacuum to reclaim space
VACUUM ANALYZE "InventorySnapshot";
```

### Monitor Query Performance

```sql
-- Enable timing
\timing

-- Test forecast query performance
EXPLAIN ANALYZE
SELECT * FROM "vw_rental_demand_30m_last_56d"
WHERE station_id = 'depot-001'
  AND bin_ts >= NOW() - INTERVAL '7 days'
ORDER BY bin_ts;
```

---

## Rollback Instructions

If you need to rollback this migration:

```sql
-- Drop materialized view
DROP MATERIALIZED VIEW IF EXISTS "vw_rental_demand_30m_last_56d";

-- Drop function
DROP FUNCTION IF EXISTS refresh_demand_view();

-- Drop tables (in order to respect foreign keys)
DROP TABLE IF EXISTS "RebalancingPlan";
DROP TABLE IF EXISTS "DemandForecast";
DROP TABLE IF EXISTS "InventorySnapshot";

-- Remove feature flags
DELETE FROM "SystemConfig" 
WHERE "Key" IN (
    'FORECASTING_ENABLED',
    'CAPACITY_ADVICE_ENABLED',
    'FORECAST_HORIZON_DAYS',
    'FORECAST_REFRESH_HOURS',
    'AVG_TRIP_HOURS',
    'TURNAROUND_HOURS',
    'SLA_WAIT_MINUTES'
);

-- Drop indexes
DROP INDEX IF EXISTS "IX_OrderBooking_DepotId_StartAt";
DROP INDEX IF EXISTS "IX_OrderBooking_StartAt_Status";
DROP INDEX IF EXISTS "IX_CarEV_ModelId_DepotId";
```

---

## Troubleshooting

### Issue: "REFRESH MATERIALIZED VIEW CONCURRENTLY" fails

**Cause:** Concurrent refresh requires a UNIQUE index.

**Solution:**
```sql
-- Add unique index
CREATE UNIQUE INDEX ix_vw_demand_unique 
ON "vw_rental_demand_30m_last_56d"(station_id, vehicle_type, bin_ts);

-- Then retry refresh
REFRESH MATERIALIZED VIEW CONCURRENTLY "vw_rental_demand_30m_last_56d";
```

### Issue: Slow materialized view refresh

**Cause:** Too much historical data (>56 days).

**Solution:**
```sql
-- Clean up old bookings first
DELETE FROM "OrderBooking" 
WHERE "StartAt" < NOW() - INTERVAL '90 days' 
  AND "Status" = 'COMPLETED';

-- Then refresh
REFRESH MATERIALIZED VIEW "vw_rental_demand_30m_last_56d";
```

### Issue: Foreign key constraint violation

**Cause:** Missing depot or model records.

**Solution:**
```sql
-- Check for orphaned records
SELECT DISTINCT ob."DepotId" 
FROM "OrderBooking" ob
LEFT JOIN "Depot" d ON ob."DepotId" = d."Id"
WHERE d."Id" IS NULL;

-- Fix by adding missing depot or deleting orphaned bookings
```

---

## Testing

### Verify Migration Success

```bash
# Run integration tests
cd EVSRS.API
dotnet test --filter "FullyQualifiedName~ForecastingIntegrationTests"

# Verify data integrity
psql -U your_db_user -d evsrs_db << EOF
SELECT 'InventorySnapshot' AS table_name, COUNT(*) AS row_count FROM "InventorySnapshot"
UNION ALL
SELECT 'DemandForecast', COUNT(*) FROM "DemandForecast"
UNION ALL
SELECT 'RebalancingPlan', COUNT(*) FROM "RebalancingPlan"
UNION ALL
SELECT 'vw_rental_demand_30m_last_56d', COUNT(*) FROM "vw_rental_demand_30m_last_56d";
EOF
```

---

## Support

For issues or questions:
1. Check logs: `/var/log/refresh_demand.log`
2. Review EF Core migrations: `EVSRS.BusinessObjects/Migrations/`
3. Consult RFC: `docs/rfc-forecast-capacity.md`

---

**Migration completed successfully!** 🎉
