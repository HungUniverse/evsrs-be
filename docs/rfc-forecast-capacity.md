# RFC: Demand Forecast & Capacity Advice System

**Status:** Draft  
**Author:** EVSRS Development Team  
**Created:** 2025-11-08  
**Last Updated:** 2025-11-08

---

## 1. Mục tiêu (Objectives)

Xây dựng hệ thống dự báo nhu cầu thuê xe điện (EV rentals) và tư vấn năng lực (capacity advice) nhằm:

1. **Dự báo nhu cầu** theo từng **30 phút / trạm / loại xe** dựa trên dữ liệu lịch sử
2. **Đề xuất số xe cần bổ sung** hoặc **điều phối lại** giữa các trạm để đảm bảo:
   - **SLA thời gian chờ < 5 phút** (wait time SLA)
   - Tối ưu hóa tỷ lệ xe sẵn sàng (availability rate)
3. **Tích hợp LLM Advisor** để cung cấp lời khuyên bằng ngôn ngữ tự nhiên về:
   - Xu hướng nhu cầu
   - Hành động điều phối khuyến nghị
   - Giải thích các quyết định capacity planning

### Business Impact
- Giảm tỷ lệ mất khách hàng do không có xe (stock-out rate)
- Tối ưu chi phí vận hành bằng cách giảm xe nhàn rỗi
- Cải thiện trải nghiệm khách hàng với wait time < 5 phút

---

## 2. Input Dữ liệu (Data Sources)

### 2.1 Existing Tables (Current Schema)

#### OrderBooking (Rentals)
```sql
-- Table: OrderBooking
-- Cột quan trọng cho forecast:
- Id (PK)
- UserId (FK → User)
- DepotId (FK → Depot)           -- Station/location
- CarEVDetailId (FK → CarEV)      -- Vehicle instance
- StartAt (timestamptz)           -- Rental start time
- EndAt (timestamptz)             -- Rental end time  
- Status (enum)                   -- PENDING, CONFIRMED, IN_USE, RETURNED, COMPLETED, CANCELLED
- CreatedAt (timestamptz)
- UpdatedAt (timestamptz)
```

#### CarEV (Vehicle Inventory)
```sql
-- Table: CarEV
- Id (PK)
- ModelId (FK → Model)            -- Vehicle type/model
- DepotId (FK → Depot)            -- Current station
- Status (enum)                   -- AVAILABLE, RESERVED, IN_USE, MAINTENANCE
- LicensePlate
- CreatedAt, UpdatedAt
```

#### Model (Vehicle Types)
```sql
-- Table: Model
- Id (PK)
- Name                            -- e.g., "Tesla Model 3", "VinFast VF8"
- ManufacturerCarId (FK)
- Price (daily rate)
- Capacity (seats)
```

#### Depot (Stations)
```sql
-- Table: Depot
- Id (PK)
- Name
- Address
- Latitude, Longitude
- Capacity (total parking spots)
```

### 2.2 New Tables (To Be Added)

#### InventorySnapshot (Time-series Snapshots)
```sql
CREATE TABLE "InventorySnapshot" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "DepotId" VARCHAR(255) NOT NULL,
    "ModelId" VARCHAR(255) NOT NULL,
    "SnapshotTime" TIMESTAMPTZ NOT NULL,
    "AvailableCount" INT NOT NULL DEFAULT 0,
    "ChargingCount" INT NOT NULL DEFAULT 0,
    "MaintenanceCount" INT NOT NULL DEFAULT 0,
    "InUseCount" INT NOT NULL DEFAULT 0,
    "ReservedCount" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy" VARCHAR(255),
    
    CONSTRAINT "FK_InventorySnapshot_Depot" 
        FOREIGN KEY ("DepotId") REFERENCES "Depot"("Id"),
    CONSTRAINT "FK_InventorySnapshot_Model" 
        FOREIGN KEY ("ModelId") REFERENCES "Model"("Id")
);

CREATE INDEX "IX_InventorySnapshot_DepotId_Time" 
    ON "InventorySnapshot"("DepotId", "SnapshotTime" DESC);
CREATE INDEX "IX_InventorySnapshot_ModelId_Time" 
    ON "InventorySnapshot"("ModelId", "SnapshotTime" DESC);
```

**Purpose:**  
Lưu trạng thái kho xe theo từng 30 phút để phân tích xu hướng, phát hiện peak hours, và tính toán capacity gaps.

#### DemandForecast (Predicted Demand)
```sql
CREATE TABLE "DemandForecast" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "DepotId" VARCHAR(255) NOT NULL,
    "ModelId" VARCHAR(255),                    -- NULL = tất cả loại xe
    "ForecastTime" TIMESTAMPTZ NOT NULL,       -- Thời điểm dự báo cho
    "PredictedDemand" DECIMAL(10,2) NOT NULL,  -- Số lượt thuê dự kiến
    "ConfidenceScore" DECIMAL(5,4),            -- 0-1, độ tin cậy
    "Method" VARCHAR(50),                      -- 'MA', 'P90', 'ML_Model'
    "GeneratedAt" TIMESTAMPTZ NOT NULL,        -- Thời điểm tạo forecast
    "HorizonMinutes" INT NOT NULL DEFAULT 30,  -- Độ dài khung thời gian
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_DemandForecast_Depot" 
        FOREIGN KEY ("DepotId") REFERENCES "Depot"("Id"),
    CONSTRAINT "FK_DemandForecast_Model" 
        FOREIGN KEY ("ModelId") REFERENCES "Model"("Id")
);

CREATE INDEX "IX_DemandForecast_DepotId_ForecastTime" 
    ON "DemandForecast"("DepotId", "ForecastTime" ASC);
```

#### RebalancingPlan (Capacity Advice & Actions)
```sql
CREATE TABLE "RebalancingPlan" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "PlanDate" DATE NOT NULL,
    "FromDepotId" VARCHAR(255),                -- NULL nếu mua mới
    "ToDepotId" VARCHAR(255) NOT NULL,
    "ModelId" VARCHAR(255) NOT NULL,
    "Quantity" INT NOT NULL,
    "ActionType" VARCHAR(50) NOT NULL,         -- 'RELOCATE', 'PURCHASE', 'MAINTENANCE'
    "Priority" INT DEFAULT 0,                  -- 0=low, 100=urgent
    "EstimatedCost" DECIMAL(18,2),
    "Status" VARCHAR(50) DEFAULT 'PROPOSED',   -- PROPOSED, APPROVED, EXECUTED, CANCELLED
    "Reason" TEXT,                             -- Lý do đề xuất
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy" VARCHAR(255),
    "ApprovedAt" TIMESTAMPTZ,
    "ApprovedBy" VARCHAR(255),
    
    CONSTRAINT "FK_RebalancingPlan_FromDepot" 
        FOREIGN KEY ("FromDepotId") REFERENCES "Depot"("Id"),
    CONSTRAINT "FK_RebalancingPlan_ToDepot" 
        FOREIGN KEY ("ToDepotId") REFERENCES "Depot"("Id"),
    CONSTRAINT "FK_RebalancingPlan_Model" 
        FOREIGN KEY ("ModelId") REFERENCES "Model"("Id")
);

CREATE INDEX "IX_RebalancingPlan_PlanDate_Status" 
    ON "RebalancingPlan"("PlanDate", "Status");
```

---

## 3. Phương pháp Dự báo (Forecasting Method)

### 3.1 Baseline Algorithm: Moving Average + P90

**Rationale:**  
Bắt đầu với phương pháp đơn giản, dễ giải thích, không cần ML training phức tạp.

**Steps:**

1. **Phân nhóm theo Slot-In-Week:**
   - Mỗi slot = (day_of_week, hour, minute_bucket_30)
   - Ví dụ: `(Monday, 08:00-08:30)`, `(Friday, 18:00-18:30)`

2. **Tính MA (Moving Average) 8 tuần gần nhất:**
   ```
   MA_slot = AVG(demand trong cùng slot 8 tuần gần nhất)
   ```

3. **Tính P90 (90th percentile) để xử lý peak:**
   ```
   P90_slot = PERCENTILE_90(demand trong cùng slot 8 tuần gần nhất)
   ```

4. **Chọn giá trị dự báo:**
   ```
   forecasted_demand = MAX(MA_slot, P90_slot * 0.85)
   ```
   _(Sử dụng P90 nhưng giảm 15% để tránh over-provision)_

### 3.2 Công thức Tính Số Xe Yêu cầu (Required Units)

```
required_units = CEIL(P90_same_slot * avg_trip_hours / turnaround_hours)
```

**Giải thích tham số:**
- `P90_same_slot`: Nhu cầu dự báo (số lượt thuê trong slot 30 phút)
- `avg_trip_hours`: Thời lượng thuê trung bình (ví dụ: 4 giờ)
- `turnaround_hours`: Thời gian chuẩn bị xe sau mỗi lượt (ví dụ: 0.5 giờ = sạc nhanh + vệ sinh)

**Ví dụ:**
```
P90 = 12 lượt thuê / 30 phút
avg_trip = 4 giờ
turnaround = 0.5 giờ

required = CEIL(12 * 4 / 0.5) = CEIL(96) = 96 xe
```

### 3.3 Công thức Tính Gap (Capacity Gap)

```
gap = MAX(0, required_units - current_available_peak_24h)
```

**Trong đó:**
- `current_available_peak_24h`: Số xe sẵn sàng tối thiểu trong 24 giờ tới (từ InventorySnapshot)
- `gap > 0`: Thiếu xe, cần điều phối hoặc mua thêm
- `gap <= 0`: Đủ xe hoặc thừa

### 3.4 SLA Calculation (Wait Time < 5 Minutes)

```
expected_wait_minutes = (required_units / current_available) * 5
sla_met = (expected_wait_minutes < 5)
```

Nếu `sla_met = false`, tăng priority của RebalancingPlan.

---

## 4. API Mới (New Endpoints)

### 4.1 GET /api/forecast/{stationId}

**Mục đích:** Lấy dự báo nhu cầu cho một trạm trong X ngày tới.

**Request:**
```http
GET /api/forecast/{stationId}?horizonDays=7&modelId=abc123
Authorization: Bearer {jwt_token}
```

**Query Parameters:**
| Param | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `horizonDays` | int | No | 7 | Số ngày dự báo (1-30) |
| `modelId` | string | No | null | Lọc theo loại xe, null = tất cả |
| `granularity` | string | No | '30min' | '30min', '1hour', '1day' |

**Response (200 OK):**
```json
{
  "stationId": "depot-001",
  "stationName": "Trạm Thủ Đức",
  "forecastStart": "2025-11-09T00:00:00Z",
  "forecastEnd": "2025-11-16T00:00:00Z",
  "granularity": "30min",
  "forecasts": [
    {
      "forecastTime": "2025-11-09T08:00:00Z",
      "predictedDemand": 8.5,
      "confidenceScore": 0.82,
      "method": "P90",
      "requiredUnits": 34,
      "currentAvailable": 28,
      "gap": 6,
      "slaMet": false
    },
    {
      "forecastTime": "2025-11-09T08:30:00Z",
      "predictedDemand": 12.3,
      "confidenceScore": 0.79,
      "method": "P90",
      "requiredUnits": 49,
      "currentAvailable": 22,
      "gap": 27,
      "slaMet": false
    }
  ],
  "summary": {
    "totalSlotsWithGap": 18,
    "maxGap": 27,
    "avgGap": 8.4,
    "slaViolationRate": 0.15
  }
}
```

**Error Responses:**
- `404 Not Found`: Station không tồn tại
- `400 Bad Request`: `horizonDays` ngoài phạm vi [1, 30]
- `401 Unauthorized`: Token không hợp lệ

---

### 4.2 GET /api/capacity-advice

**Mục đích:** Lấy danh sách khuyến nghị điều phối xe cho một ngày cụ thể.

**Request:**
```http
GET /api/capacity-advice?date=2025-11-10&status=PROPOSED
Authorization: Bearer {jwt_token}
```

**Query Parameters:**
| Param | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `date` | string | Yes | - | YYYY-MM-DD |
| `status` | string | No | 'PROPOSED' | 'PROPOSED', 'APPROVED', 'EXECUTED', 'ALL' |
| `minPriority` | int | No | 0 | Lọc priority >= giá trị này |

**Response (200 OK):**
```json
{
  "planDate": "2025-11-10",
  "generatedAt": "2025-11-08T10:30:00Z",
  "plans": [
    {
      "id": "plan-12345",
      "actionType": "RELOCATE",
      "fromStation": {
        "id": "depot-002",
        "name": "Trạm Quận 1"
      },
      "toStation": {
        "id": "depot-001",
        "name": "Trạm Thủ Đức"
      },
      "vehicleType": {
        "id": "model-tesla-3",
        "name": "Tesla Model 3"
      },
      "quantity": 6,
      "priority": 85,
      "estimatedCost": 1200000,
      "reason": "Peak demand at Thủ Đức 08:00-10:00, surplus at Quận 1",
      "status": "PROPOSED",
      "createdAt": "2025-11-08T10:30:00Z"
    },
    {
      "id": "plan-12346",
      "actionType": "PURCHASE",
      "fromStation": null,
      "toStation": {
        "id": "depot-001",
        "name": "Trạm Thủ Đức"
      },
      "vehicleType": {
        "id": "model-vinfast-vf8",
        "name": "VinFast VF8"
      },
      "quantity": 3,
      "priority": 60,
      "estimatedCost": 2400000000,
      "reason": "Sustained demand growth (>20% in 4 weeks), no surplus at other stations",
      "status": "PROPOSED",
      "createdAt": "2025-11-08T10:30:00Z"
    }
  ],
  "summary": {
    "totalActions": 2,
    "totalRelocations": 1,
    "totalPurchases": 1,
    "totalVehicles": 9,
    "totalEstimatedCost": 2401200000
  }
}
```

---

### 4.3 POST /api/capacity-constraints

**Mục đích:** Cấu hình ràng buộc ngân sách và SLA cho capacity planning.

**Request:**
```http
POST /api/capacity-constraints
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "monthlyBudget": 50000000000,
  "maxDailyPurchase": 10,
  "slaWaitMinutes": 5,
  "turnaroundHours": 0.5,
  "avgTripHours": 4.0,
  "minFleetPerStation": 15,
  "maxRelocationCostPerUnit": 200000,
  "effectiveFrom": "2025-11-01",
  "effectiveTo": "2025-11-30"
}
```

**Response (200 OK):**
```json
{
  "id": "constraint-001",
  "message": "Capacity constraints updated successfully",
  "appliedAt": "2025-11-08T10:35:00Z"
}
```

**Validation Rules:**
- `monthlyBudget` >= 0
- `maxDailyPurchase` >= 0, <= 100
- `slaWaitMinutes` > 0, <= 60
- `turnaroundHours` >= 0.25, <= 2
- `avgTripHours` >= 0.5, <= 240

---

## 5. Ràng buộc Kỹ thuật (Technical Constraints)

### 5.1 Không Chặn Luồng Cũ (Non-Breaking)

- **Feature Flag:** Sử dụng `SystemConfig` table để bật/tắt forecasting:
  ```sql
  INSERT INTO "SystemConfig" (Id, Key, Value, Description) VALUES
  ('cfg-forecast-enabled', 'FORECASTING_ENABLED', 'true', 'Enable demand forecasting'),
  ('cfg-capacity-advice-enabled', 'CAPACITY_ADVICE_ENABLED', 'false', 'Enable capacity advice API');
  ```

- **Backward Compatibility:**
  - Các API cũ (`/api/order`, `/api/car`, etc.) không bị ảnh hưởng
  - ForecastingService chạy độc lập, không can thiệp vào OrderBookingService
  - Nếu feature flag = `false`, API trả về `501 Not Implemented`

### 5.2 Performance

- **Background Job:** Forecasting chạy mỗi 6 giờ qua `IHostedService`, không blocking user requests
- **Caching:** Cache forecast results 30 phút trong Redis hoặc in-memory
- **Database:**
  - Index trên `(DepotId, SnapshotTime)` và `(DepotId, ForecastTime)`
  - Partition `InventorySnapshot` theo tháng nếu data > 1 triệu rows

### 5.3 Scalability

- **Data Retention:**
  - `InventorySnapshot`: Giữ 90 ngày, archive sau đó
  - `DemandForecast`: Xóa forecast cũ hơn 30 ngày
  - `RebalancingPlan`: Giữ vô hạn (audit trail)

---

## 6. Bảo mật (Security)

### 6.1 API Key Management

- **OPENAI_API_KEY:** Lưu trong `appsettings.json` (Development) hoặc Azure Key Vault (Production)
  ```json
  {
    "LlmSettings": {
      "Provider": "OpenAI",
      "ApiKey": "{HIDDEN}",
      "Model": "gpt-4",
      "MaxTokens": 500,
      "Temperature": 0.7
    }
  }
  ```

- **IOptions Pattern:**
  ```csharp
  public class LlmSettings
  {
      public string Provider { get; set; }
      public string ApiKey { get; set; }
      public string Model { get; set; }
      public int MaxTokens { get; set; }
      public double Temperature { get; set; }
  }
  
  // In DI:
  services.Configure<LlmSettings>(configuration.GetSection("LlmSettings"));
  ```

### 6.2 Authorization

- **Endpoints `/api/forecast/*` và `/api/capacity-advice`:**
  - Yêu cầu role `ADMIN` hoặc `MANAGER`
  - Staff chỉ xem được forecast của depot mình phụ trách

- **Endpoint `/api/capacity-constraints`:**
  - Chỉ `ADMIN` mới có quyền POST (update constraints)

---

## 7. Kiểm thử (Testing Strategy)

### 7.1 Unit Tests

**Test Cases cho `ForecastingService`:**

```csharp
[Fact]
public async Task GetForecastAsync_WithValidData_ReturnsP90Forecast()
{
    // Arrange
    var mockRepo = CreateMockOrderRepository();
    var service = new ForecastingService(mockRepo, ...);
    
    // Act
    var result = await service.GetForecastAsync("depot-001", DateTime.UtcNow, TimeSpan.FromDays(7));
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.Forecasts.Count > 0);
    Assert.All(result.Forecasts, f => Assert.InRange(f.ConfidenceScore, 0, 1));
}

[Fact]
public void CalculateRequiredUnits_WithP90AndAvgTrip_ReturnsCorrectValue()
{
    // Arrange
    decimal p90 = 12m;
    decimal avgTripHours = 4m;
    decimal turnaroundHours = 0.5m;
    
    // Act
    int required = CapacityPlanner.CalculateRequiredUnits(p90, avgTripHours, turnaroundHours);
    
    // Assert
    Assert.Equal(96, required); // CEIL(12 * 4 / 0.5)
}
```

**Test Cases cho `CapacityPlanner`:**

```csharp
[Fact]
public async Task GetCapacityPlanAsync_WhenGapExists_ProposesRebalancing()
{
    // Arrange
    var mockInventory = CreateMockInventoryWithGap();
    var planner = new CapacityPlanner(mockInventory, ...);
    
    // Act
    var plan = await planner.GetCapacityPlanAsync("depot-001", DateTime.Today, TimeSpan.FromDays(1));
    
    // Assert
    Assert.True(plan.Plans.Count > 0);
    Assert.Contains(plan.Plans, p => p.ActionType == "RELOCATE" || p.ActionType == "PURCHASE");
}
```

### 7.2 Integration Tests

**Sử dụng TestContainers Postgres:**

```csharp
[Collection("Database")]
public class ForecastingIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _container;
    private ApplicationDbContext _dbContext;
    
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .Build();
        await _container.StartAsync();
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.MigrateAsync();
        
        // Seed test data
        await SeedTestData();
    }
    
    [Fact]
    public async Task E2E_ForecastEndpoint_ReturnsValidJson()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetAdminToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Act
        var response = await client.GetAsync("/api/forecast/depot-001?horizonDays=3");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var forecast = JsonSerializer.Deserialize<ForecastResponse>(json);
        
        Assert.NotNull(forecast);
        Assert.Equal("depot-001", forecast.StationId);
        Assert.True(forecast.Forecasts.Count >= 144); // 3 days * 48 slots/day
    }
    
    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _container.StopAsync();
    }
}
```

### 7.3 Performance Tests

**Load Test Scenario:**
```csharp
[Fact]
public async Task Benchmark_ForecastGeneration_Under2Seconds()
{
    // 1000 historical bookings, 7-day forecast
    var stopwatch = Stopwatch.StartNew();
    
    var result = await _forecastingService.GetForecastAsync("depot-001", DateTime.UtcNow, TimeSpan.FromDays(7));
    
    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Took {stopwatch.ElapsedMilliseconds}ms");
}
```

---

## 8. Acceptance Criteria

### 8.1 Functional Requirements

- [ ] **FR-1:** API `/api/forecast/{stationId}` trả về dự báo cho 1-30 ngày, granularity 30 phút
- [ ] **FR-2:** Dự báo sử dụng phương pháp P90 trên 8 tuần lịch sử
- [ ] **FR-3:** Công thức `required_units = CEIL(P90 * avg_trip / turnaround)` được áp dụng đúng
- [ ] **FR-4:** API `/api/capacity-advice` trả về danh sách actions (RELOCATE, PURCHASE) với priority
- [ ] **FR-5:** `gap > 0` tạo RebalancingPlan với status PROPOSED
- [ ] **FR-6:** SLA wait time < 5 phút được tính toán và hiển thị trong response
- [ ] **FR-7:** Feature flag `FORECASTING_ENABLED` cho phép bật/tắt mà không deploy lại

### 8.2 Non-Functional Requirements

- [ ] **NFR-1:** API response time < 500ms (P95) cho `/api/forecast`
- [ ] **NFR-2:** Background forecast job hoàn thành trong < 5 phút cho toàn bộ hệ thống (50 trạm)
- [ ] **NFR-3:** OPENAI_API_KEY không xuất hiện trong logs hoặc error messages
- [ ] **NFR-4:** Unit test coverage >= 80% cho ForecastingService và CapacityPlanner
- [ ] **NFR-5:** Integration tests chạy thành công với Testcontainers Postgres
- [ ] **NFR-6:** API `/api/capacity-constraints` chỉ cho phép ADMIN POST

### 8.3 Data Quality

- [ ] **DQ-1:** `InventorySnapshot` được tạo mỗi 30 phút cho tất cả depot × model combinations
- [ ] **DQ-2:** `DemandForecast.ConfidenceScore` trong khoảng [0, 1]
- [ ] **DQ-3:** `RebalancingPlan.Quantity > 0` và `EstimatedCost >= 0`
- [ ] **DQ-4:** Historical data cũ hơn 90 ngày được archive (không xóa hoàn toàn)

---

## 9. Roadmap & Future Enhancements

### Phase 1 (Current RFC)
- ✅ Baseline P90/MA forecasting
- ✅ Simple capacity gap calculation
- ✅ REST APIs for forecast & advice
- ✅ Feature flag support

### Phase 2 (Next 3 Months)
- [ ] ML-based forecasting (LSTM, Prophet) với seasonality detection
- [ ] Real-time demand adjustment dựa trên weather API
- [ ] LLM Advisor integration (GPT-4 hoặc Claude) cho natural language insights
- [ ] Dashboard UI cho ADMIN/MANAGER xem trends và approve plans

### Phase 3 (6-12 Months)
- [ ] Multi-objective optimization (cost + SLA + carbon footprint)
- [ ] Automated rebalancing execution (tích hợp logistics API)
- [ ] Predictive maintenance alerts (dựa trên usage patterns)

---

## 10. References

- [Time Series Forecasting Best Practices (Microsoft)](https://github.com/microsoft/forecasting)
- [Capacity Planning for SaaS (Google SRE Book)](https://sre.google/sre-book/capacity-planning/)
- [EF Core Performance Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)

---

## 11. Approval & Sign-off

| Role | Name | Status | Date |
|------|------|--------|------|
| Tech Lead | [TBD] | ⏳ Pending | - |
| Product Owner | [TBD] | ⏳ Pending | - |
| Security Review | [TBD] | ⏳ Pending | - |
| Architect | [TBD] | ⏳ Pending | - |

---

**END OF RFC**
