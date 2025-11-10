# Demand Forecasting & Capacity Planning - Implementation Summary

## 📋 Overview
Migration package for adding demand forecasting and capacity planning features to EVSRS system.

---

## 📁 Files Created

### 1. Database Migration Files
```
EVSRS.BusinessObjects/Migrations/SQL/
├── 001_create_demand_forecast_tables.sql    # Main migration script (idempotent)
└── refresh_demand_view.sql                  # Materialized view refresh script
```

### 2. Entity Classes
```
EVSRS.BusinessObjects/Entity/
├── InventorySnapshot.cs                     # Time-series vehicle inventory snapshots
├── DemandForecast.cs                        # Predicted demand per station/vehicle type
└── RebalancingPlan.cs                       # Capacity rebalancing recommendations
```

### 3. DbContext Updates
```
EVSRS.BusinessObjects/DBContext/
└── ApplicationDbContext.cs                  # Added 3 new DbSets + Fluent API configs
```

### 4. Documentation
```
docs/
├── rfc-forecast-capacity.md                 # RFC document (full specification)
└── migration-guide.md                       # Migration & deployment guide
```

---

## 🗄️ Database Schema

### New Tables

#### InventorySnapshot
Stores vehicle availability snapshots every 30 minutes.
```sql
- DepotId (FK → Depot)
- ModelId (FK → Model)  
- SnapshotTime (timestamptz, indexed)
- AvailableCount, ChargingCount, MaintenanceCount, InUseCount, ReservedCount
```

#### DemandForecast
Stores predicted demand for future time slots.
```sql
- DepotId (FK → Depot)
- ModelId (FK → Model, nullable)
- ForecastTime (timestamptz, indexed)
- PredictedDemand (decimal)
- ConfidenceScore (decimal 0-1)
- Method (varchar: 'MA', 'P90', 'LSTM', etc.)
```

#### RebalancingPlan
Stores capacity rebalancing recommendations.
```sql
- PlanDate (date, indexed)
- FromDepotId (FK → Depot, nullable)
- ToDepotId (FK → Depot)
- ModelId (FK → Model)
- Quantity (int, CHECK > 0)
- ActionType (varchar: 'RELOCATE', 'PURCHASE', 'MAINTENANCE')
- Priority (int 0-100)
- Status (varchar: 'PROPOSED', 'APPROVED', 'EXECUTED', 'CANCELLED')
```

### Materialized View

#### vw_rental_demand_30m_last_56d
Aggregates rental demand in 30-minute bins over last 56 days.
```sql
SELECT 
    station_id (DepotId),
    vehicle_type (ModelId),
    bin_ts (30-minute truncated timestamp),
    COUNT(*) AS demand
FROM OrderBooking (last 56 days)
GROUP BY station_id, vehicle_type, bin_ts
```

**Refresh:** Every 6 hours via cron/pg_cron

---

## 🚀 Quick Start

### 1. Run Migration

```bash
# Navigate to project root
cd /Users/namnguyenle/Documents/VaiThuLinhTinhDaiHoc/SWP-Hung/evsrs-be

# Run SQL migration
psql -U your_username -d evsrs_db -f EVSRS.BusinessObjects/Migrations/SQL/001_create_demand_forecast_tables.sql

# Initial refresh of materialized view
psql -U your_username -d evsrs_db -f EVSRS.BusinessObjects/Migrations/SQL/refresh_demand_view.sql
```

### 2. Enable Feature Flags

```sql
UPDATE "SystemConfig" SET "Value" = 'true' WHERE "Key" = 'FORECASTING_ENABLED';
UPDATE "SystemConfig" SET "Value" = 'true' WHERE "Key" = 'CAPACITY_ADVICE_ENABLED';
```

### 3. Schedule View Refresh (Cron)

```bash
crontab -e
# Add:
0 */6 * * * psql -U your_username -d evsrs_db -f /path/to/refresh_demand_view.sql
```

---

## 📊 Indexes Created

### On Existing Tables
```sql
IX_OrderBooking_DepotId_StartAt          -- For demand analysis
IX_OrderBooking_StartAt_Status           -- For historical queries
IX_CarEV_ModelId_DepotId                 -- For vehicle type lookups
```

### On New Tables
```sql
IX_InventorySnapshot_DepotId_Time        -- Time-series queries
IX_InventorySnapshot_ModelId_Time        -- Vehicle type filtering
IX_DemandForecast_DepotId_ForecastTime   -- Forecast lookup
IX_DemandForecast_ModelId_ForecastTime   -- Model-specific forecast
IX_RebalancingPlan_PlanDate_Status       -- Plan management
IX_RebalancingPlan_ToDepotId             -- Destination depot filtering
```

### On Materialized View
```sql
IX_vw_demand_station_bin                 -- Station + time lookup
IX_vw_demand_vehicle_bin                 -- Vehicle type + time lookup
IX_vw_demand_bin_ts                      -- Time-based sorting
```

---

## 🔒 Feature Flags (SystemConfig)

| Key | Default | Description |
|-----|---------|-------------|
| `FORECASTING_ENABLED` | `false` | Enable demand forecasting feature |
| `CAPACITY_ADVICE_ENABLED` | `false` | Enable capacity advice API |
| `FORECAST_HORIZON_DAYS` | `7` | Default forecast horizon in days |
| `FORECAST_REFRESH_HOURS` | `6` | Hours between forecast refresh |
| `AVG_TRIP_HOURS` | `4.0` | Average trip duration |
| `TURNAROUND_HOURS` | `0.5` | Vehicle turnaround time |
| `SLA_WAIT_MINUTES` | `5` | Target SLA wait time |

---

## 🧪 Verification

### Check Tables
```sql
SELECT tablename FROM pg_tables 
WHERE tablename IN ('InventorySnapshot', 'DemandForecast', 'RebalancingPlan');
```

### Check Materialized View
```sql
SELECT COUNT(*) FROM vw_rental_demand_30m_last_56d;
```

### Check Indexes
```sql
SELECT indexname FROM pg_indexes 
WHERE tablename LIKE 'Inventory%' OR tablename LIKE 'Demand%' OR tablename LIKE 'Rebalancing%';
```

### Verify Data Flow
```sql
-- Should return data if you have bookings in last 56 days
SELECT 
    station_id, 
    vehicle_type, 
    DATE_TRUNC('day', bin_ts) AS day,
    SUM(demand) AS daily_demand
FROM vw_rental_demand_30m_last_56d
GROUP BY station_id, vehicle_type, DATE_TRUNC('day', bin_ts)
ORDER BY day DESC
LIMIT 10;
```

---

## 🐳 Optional: TimescaleDB

For better time-series performance:

```sql
CREATE EXTENSION IF NOT EXISTS timescaledb;

SELECT create_hypertable(
    '"InventorySnapshot"',
    '"SnapshotTime"',
    if_not_exists => TRUE,
    chunk_time_interval => INTERVAL '7 days'
);

SELECT add_compression_policy('"InventorySnapshot"', INTERVAL '30 days');
SELECT add_retention_policy('"InventorySnapshot"', INTERVAL '90 days');
```

---

## 📝 Next Steps

### Phase 1: Data Collection (Current)
- ✅ Database schema and tables
- ✅ Materialized view for historical analysis
- ✅ Indexes for query performance
- ⏳ Background job to populate InventorySnapshot (every 30 min)

### Phase 2: Service Implementation (Next)
- [ ] Create `IForecastingService` and implementation
- [ ] Create `ICapacityPlanner` and implementation
- [ ] Create `ILlmAdvisor` for natural language insights
- [ ] Add repositories (IInventoryRepository, etc.)
- [ ] Register services in DI

### Phase 3: API Endpoints
- [ ] `GET /api/forecast/{stationId}?horizonDays=7`
- [ ] `GET /api/capacity-advice?date=YYYY-MM-DD`
- [ ] `POST /api/capacity-constraints`
- [ ] Add `ForecastController`

### Phase 4: Background Jobs
- [ ] Implement `IHostedService` for periodic forecasting
- [ ] Auto-generate RebalancingPlan based on forecasts
- [ ] Auto-refresh materialized view

### Phase 5: Testing & Optimization
- [ ] Unit tests for ForecastingService
- [ ] Integration tests with Testcontainers
- [ ] Performance benchmarks
- [ ] ML model integration (Prophet, LSTM)

---

## 🔄 Rollback

If needed, run:

```sql
DROP MATERIALIZED VIEW IF EXISTS "vw_rental_demand_30m_last_56d";
DROP FUNCTION IF EXISTS refresh_demand_view();
DROP TABLE IF EXISTS "RebalancingPlan";
DROP TABLE IF EXISTS "DemandForecast";
DROP TABLE IF EXISTS "InventorySnapshot";
DELETE FROM "SystemConfig" WHERE "Key" LIKE '%FORECAST%' OR "Key" LIKE '%CAPACITY%';
```

---

## 📚 References

- **RFC Document:** `docs/rfc-forecast-capacity.md`
- **Migration Guide:** `docs/migration-guide.md`
- **SQL Scripts:** `EVSRS.BusinessObjects/Migrations/SQL/`
- **Entity Classes:** `EVSRS.BusinessObjects/Entity/`

---

## ✅ Migration Checklist

- [x] SQL migration script created and tested
- [x] Entity classes defined with navigation properties
- [x] DbContext updated with new DbSets
- [x] Fluent API configurations added
- [x] Indexes created for performance
- [x] Materialized view created
- [x] Refresh function/script created
- [x] Feature flags seeded in SystemConfig
- [x] Build succeeds (0 errors)
- [x] Documentation complete (RFC + Migration Guide)
- [ ] Migration applied to development database
- [ ] Migration applied to staging database
- [ ] Migration applied to production database
- [ ] Background jobs scheduled (view refresh, snapshot capture)
- [ ] Monitoring/alerting configured

---

**Status:** ✅ Ready for deployment  
**Build:** ✅ 0 Errors, 137 Warnings (null reference only)  
**Idempotent:** ✅ Safe to re-run migration script  
**Rollback:** ✅ Rollback script provided
