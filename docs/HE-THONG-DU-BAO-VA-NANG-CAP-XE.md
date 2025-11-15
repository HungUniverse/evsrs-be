# 📊 HỆ THỐNG Dự BÁO NHU CẦU VÀ NÂNG CẤP XE TẠI DEPOT

> **Mục đích**: Hệ thống tự động dự báo nhu cầu thuê xe và đề xuất phương án điều chỉnh số lượng xe tại các depot để đáp ứng nhu cầu khách hàng.

---

## 📑 MỤC LỤC

1. [Tổng quan hệ thống](#-tng-quan-h-thng)
2. [Kiến trúc tổng thể](#-kin-trúc-tng-th)
3. [Phần 1: Dự báo nhu cầu (Demand Forecasting)](#-phn-1-d-báo-nhu-cu-demand-forecasting)
4. [Phần 2: Tính toán sức chứa (Capacity Planning)](#-phn-2-tính-toán-sc-cha-capacity-planning)
5. [Phần 3: Đề xuất điều chỉnh (Rebalancing Planning)](#-phn-3--xut-iu-chnh-rebalancing-planning)
6. [Luồng hoạt động End-to-End](#-lung-hot-ng-end-to-end)
7. [Cơ sở dữ liệu](#-c-s-d-liu)
8. [API Endpoints](#-api-endpoints)

---

## 🎯 TỔNG QUAN HỆ THỐNG

### Bài toán cần giải quyết

**Vấn đề**:
- Depot A có quá nhiều xe nhàn rỗi (thừa)
- Depot B thiếu xe, không đủ đáp ứng nhu cầu khách hàng
- Cần biết nên mua thêm bao nhiêu xe và loại nào
- Cần biết nên di chuyển xe giữa các depot như thế nào

**Giải pháp**:
1. **Dự báo nhu cầu**: Phân tích lịch sử booking để dự đoán nhu cầu tương lai
2. **Tính toán công suất**: So sánh nhu cầu dự báo với số xe hiện có
3. **Đề xuất điều chỉnh**: Tự động đề xuất di chuyển xe hoặc mua thêm xe

---

## 🏗️ KIẾN TRÚC TỔNG THỂ

```
┌──────────────────────────────────────────────────────────────┐
│                    HỆ THỐNG DỰ BÁO & NÂNG CẤP                │
└──────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐    ┌────────────────┐    ┌─────────────────┐
│   FORECASTING │───▶│    CAPACITY    │───▶│   REBALANCING   │
│    SERVICE    │    │    PLANNING    │    │     PLANNER     │
└───────────────┘    └────────────────┘    └─────────────────┘
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────────────────────────────────────────────────────┐
│                      DATABASE LAYERS                          │
├───────────────┬────────────────┬──────────────────────────────┤
│ Materialized  │ DemandForecast │     RebalancingPlan          │
│     View      │     Table      │         Table                │
└───────────────┴────────────────┴──────────────────────────────┘
```

### Các thành phần chính

| Thành phần | Chức năng | Tần suất chạy |
|------------|-----------|---------------|
| **ForecastingService** | Tính toán thống kê nhu cầu (Mean, P90) | On-demand |
| **DemandForecastGeneratorService** | Tạo dự báo nhu cầu 24h tới | 6 giờ/lần |
| **RebalancingPlannerService** | Đề xuất điều chỉnh xe | 12 giờ/lần |
| **MaterializedViewRefreshService** | Cập nhật dữ liệu lịch sử | 1 giờ/lần |

---

## 📈 PHẦN 1: DỰ BÁO NHU CẦU (DEMAND FORECASTING)

### 1.1. Nguồn dữ liệu

#### Materialized View: `vw_rental_demand_30m_last_56d`
```sql
-- Lưu nhu cầu booking theo khung giờ 30 phút trong 56 ngày gần nhất
CREATE MATERIALIZED VIEW vw_rental_demand_30m_last_56d AS
SELECT 
    depot_id AS station_id,
    model_id AS vehicle_type,
    DATE_BIN('30 minutes', "StartAt", '1970-01-01'::timestamp) AS bin_ts,
    COUNT(*) AS demand
FROM "OrderBooking"
WHERE "StartAt" >= NOW() - INTERVAL '56 days'
  AND "Status" NOT IN ('CANCELLED', 'REFUND_PENDING')
GROUP BY depot_id, model_id, bin_ts;
```

**Ý nghĩa**:
- Chia timeline thành các khung 30 phút (7:00-7:30, 7:30-8:00, ...)
- Đếm số booking bắt đầu trong mỗi khung giờ
- Lưu lịch sử 56 ngày (8 tuần) để phân tích xu hướng

**Ví dụ dữ liệu**:
| station_id | vehicle_type | bin_ts | demand |
|------------|--------------|--------|--------|
| depot-001 | tesla-model-3 | 2025-11-10 07:00:00 | 3 |
| depot-001 | tesla-model-3 | 2025-11-10 07:30:00 | 5 |
| depot-002 | vf8 | 2025-11-10 08:00:00 | 2 |

---

### 1.2. Thuật toán dự báo: P90 (Percentile 90)

#### Tại sao dùng P90?
- **Mean (Trung bình)**: Nhạy cảm với outliers, không đủ an toàn
- **P90**: Đảm bảo đáp ứng được 90% trường hợp
- **P95/P99**: Quá dư thừa, tốn chi phí

#### Công thức tính P90

```csharp
// File: ForecastingService.cs
private static double CalculateQuantile(double[] sortedValues, double p)
{
    if (sortedValues.Length == 0) return 0;
    if (sortedValues.Length == 1) return sortedValues[0];
    
    double pos = (sortedValues.Length - 1) * p;  // Vị trí trong mảng
    int lower = (int)Math.Floor(pos);            // Index dưới
    int upper = (int)Math.Ceiling(pos);          // Index trên
    double weight = pos - lower;                 // Trọng số nội suy
    
    // Nội suy tuyến tính giữa 2 giá trị
    return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
}
```

**Ví dụ cụ thể**:
```
Dữ liệu 8 tuần, mỗi thứ Hai 8:00-8:30 tại depot-001, Tesla Model 3:
Week 1: 3 bookings
Week 2: 5 bookings
Week 3: 4 bookings
Week 4: 6 bookings
Week 5: 2 bookings
Week 6: 7 bookings
Week 7: 4 bookings
Week 8: 5 bookings

Sắp xếp: [2, 3, 4, 4, 5, 5, 6, 7]
P90 position = (8-1) * 0.90 = 6.3
P90 = values[6] * 0.7 + values[7] * 0.3
    = 6 * 0.7 + 7 * 0.3
    = 4.2 + 2.1 = 6.3 bookings

➡️ Dự báo: 7 bookings (làm tròn lên)
```

---

### 1.3. Service: DemandForecastGeneratorService

#### Chức năng
- Chạy **mỗi 6 giờ** tự động
- Tạo dự báo cho **24 giờ tiếp theo** (48 khung 30 phút)
- Lưu vào bảng `DemandForecast`

#### Quy trình

```
1. Lấy danh sách tất cả cặp (depot, model)
   └─> GetStationVehicleTypesAsync()

2. Với mỗi cặp (depot, model):
   ├─> Lấy dữ liệu lịch sử 7 ngày gần nhất
   ├─> Tính P90 cho từng slot (giờ, phút trong tuần)
   └─> Tạo 48 bản ghi dự báo (24h * 2 slots/h)

3. Lưu vào database
   ├─> Xóa dự báo cũ cho cùng thời gian
   ├─> Insert dự báo mới
   └─> Cleanup dự báo quá 7 ngày
```

#### Code chi tiết

```csharp
// File: DemandForecastGeneratorService.cs (Line 77-130)
private async Task GenerateForecastsAsync(CancellationToken cancellationToken)
{
    // Lấy tất cả cặp (depot, model)
    var stationVehicleTypes = await forecastingService
        .GetStationVehicleTypesAsync(cancellationToken);
    
    var forecasts = new List<DemandForecast>();
    var startDate = DateTime.UtcNow.AddDays(-7); // 7 ngày lịch sử
    var endDate = DateTime.UtcNow;

    foreach (var (stationId, vehicleType) in stationVehicleTypes)
    {
        // Lấy thống kê P90 từ lịch sử
        var stats = await forecastingService.GetStatsAsync(
            stationId, vehicleType, startDate, endDate, cancellationToken);

        if (stats == null) continue; // Không có dữ liệu

        // Tạo 48 dự báo (24 giờ * 2 slots)
        for (int i = 0; i < 48; i++)
        {
            var forecastTime = DateTime.UtcNow.AddMinutes(i * 30);
            
            // Làm tròn xuống 30 phút (7:23 → 7:00, 7:34 → 7:30)
            forecastTime = new DateTime(
                forecastTime.Year, forecastTime.Month, forecastTime.Day,
                forecastTime.Hour, 
                forecastTime.Minute < 30 ? 0 : 30, 
                0, DateTimeKind.Utc);

            forecasts.Add(new DemandForecast
            {
                DepotId = stationId,
                ModelId = vehicleType,
                ForecastTime = forecastTime,
                PredictedDemand = (decimal)stats.P90,
                ConfidenceScore = CalculateConfidenceScore(stats),
                Method = "P90",
                HorizonMinutes = 30
            });
        }
    }

    if (forecasts.Any())
    {
        // Xóa dự báo cũ cho cùng time slots
        await dbContext.DemandForecasts
            .Where(f => forecastTimes.Contains(f.ForecastTime))
            .ExecuteDeleteAsync(cancellationToken);

        // Insert dự báo mới
        await dbContext.DemandForecasts.AddRangeAsync(forecasts);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

#### Ví dụ kết quả

Bảng `DemandForecast` sau khi chạy:

| DepotId | ModelId | ForecastTime | PredictedDemand | Method |
|---------|---------|--------------|-----------------|--------|
| depot-001 | tesla-3 | 2025-11-10 08:00:00 | 6.3 | P90 |
| depot-001 | tesla-3 | 2025-11-10 08:30:00 | 7.1 | P90 |
| depot-001 | tesla-3 | 2025-11-10 09:00:00 | 5.8 | P90 |
| depot-002 | vf8 | 2025-11-10 08:00:00 | 4.2 | P90 |

---

## 🔢 PHẦN 2: TÍNH TOÁN SỨC CHỨA (CAPACITY PLANNING)

### 2.1. Công thức tính số xe cần thiết

#### Input
- **P90 Demand**: Số booking dự kiến (từ phần 1)
- **Avg Trip Hours**: Thời gian thuê trung bình (mặc định: 2.0 giờ)
- **Turnaround Hours**: Thời gian vệ sinh/bảo trì giữa các chuyến (mặc định: 1.0 giờ)

#### Công thức

```
Required Units = ⌈ P90 × Avg Trip Hours / Cycle Hours ⌉

Trong đó:
- Cycle Hours = Avg Trip Hours + Turnaround Hours
- ⌈ ⌉ = hàm làm tròn lên (ceiling)
```

#### Giải thích logic

```
Giả sử:
- P90 Demand = 12 bookings/giờ
- Avg Trip = 2 giờ
- Turnaround = 1 giờ
- Cycle = 2 + 1 = 3 giờ

Trong 1 giờ:
- 12 khách muốn thuê xe
- Mỗi xe hoàn thành 1/3 chu kỳ (1 giờ / 3 giờ cycle)
- Để xử lý 12 bookings trong 1 giờ:
  Required = ⌈12 × 2 / 3⌉ = ⌈8⌉ = 8 xe

Kiểm chứng:
- 8 xe × (1 giờ / 3 giờ cycle) × (2 giờ trip / 2 giờ trip) = 8/3 ≈ 2.67 bookings/xe/giờ
- 8 xe × 2.67 = ~21 bookings có thể xử lý
- ✓ Đủ để cover 12 bookings (P90)
```

#### Code implementation

```csharp
// File: ForecastingService.cs (Line 426-433)
public int GetRequiredUnits(double p90Demand, double avgTripHours, double turnaroundHours)
{
    if (p90Demand <= 0) return 0;
    
    var cycleHours = avgTripHours + turnaroundHours;
    var required = Math.Ceiling(p90Demand * avgTripHours / cycleHours);
    return (int)required;
}
```

---

### 2.2. Tính GAP (Thiếu hụt hoặc Thừa)

#### Công thức

```
GAP = Required Units - Current Available

Nếu GAP > 0 → THIẾU xe (shortage)
Nếu GAP < 0 → THỪA xe (surplus)
Nếu GAP = 0 → CÂN BẰNG (balanced)
```

#### Ví dụ

```
Depot A - Tesla Model 3:
- P90 Demand = 12 bookings/giờ
- Required = 8 xe (từ công thức trên)
- Current Available = 5 xe
- GAP = 8 - 5 = +3 xe → THIẾU 3 xe

Depot B - Tesla Model 3:
- P90 Demand = 6 bookings/giờ
- Required = 4 xe
- Current Available = 7 xe
- GAP = 4 - 7 = -3 xe → THỪA 3 xe
```

---

### 2.3. Tính Priority (Độ ưu tiên)

#### Công thức

```csharp
Priority = MIN(100, Gap × 5 + P90 × 2)

// Cap ở 100 để không vượt quá scale 0-100
```

#### Ý nghĩa
- **Gap lớn** → priority cao (thiếu nhiều xe)
- **P90 cao** → priority cao (nhu cầu lớn)
- Scale: 0-100 (0 = không ưu tiên, 100 = cực kỳ khẩn cấp)

#### Ví dụ

```
Case 1: Thiếu 5 xe, P90 = 15
Priority = MIN(100, 5×5 + 15×2) = MIN(100, 25+30) = 55

Case 2: Thiếu 10 xe, P90 = 20
Priority = MIN(100, 10×5 + 20×2) = MIN(100, 50+40) = 90

Case 3: Thiếu 8 xe, P90 = 30
Priority = MIN(100, 8×5 + 30×2) = MIN(100, 40+60) = 100 (capped)
```

---

### 2.4. Service: CapacityRecommendation

#### Kết quả trả về

```csharp
public class CapacityRecommendation
{
    public string StationId { get; set; }           // depot-001
    public string StationName { get; set; }         // "Depot Quận 1"
    public string VehicleTypeId { get; set; }       // tesla-model-3
    public string VehicleTypeName { get; set; }     // "Tesla Model 3"
    public double PeakP90Demand { get; set; }       // 12.5
    public SlotKey PeakSlot { get; set; }           // Thứ 2, 8:00-8:30
    public int RequiredUnits { get; set; }          // 8
    public int CurrentAvailablePeak24h { get; set; }// 5
    public int Gap { get; set; }                    // +3
    public int Priority { get; set; }               // 65
    public string? RecommendedAction { get; set; }  // "RELOCATE or PURCHASE"
    public string? Reason { get; set; }             // "Gap of 3 units..."
}
```

#### Ví dụ API Response

```json
GET /api/forecasting/capacity-recommendations

{
  "recommendations": [
    {
      "stationId": "depot-001",
      "stationName": "Depot Thủ Đức",
      "vehicleTypeId": "tesla-model-3",
      "vehicleTypeName": "Tesla Model 3",
      "peakP90Demand": 12.5,
      "peakSlot": {
        "dayOfWeek": 1,
        "hour": 8,
        "minute": 0
      },
      "requiredUnits": 8,
      "currentAvailablePeak24h": 5,
      "gap": 3,
      "priority": 65,
      "recommendedAction": "RELOCATE or PURCHASE",
      "reason": "Gap of 3 units during peak demand (12.5 bookings/30min)"
    }
  ]
}
```

---

## 🔄 PHẦN 3: ĐỀ XUẤT ĐIỀU CHỈNH (REBALANCING PLANNING)

### 3.1. Service: RebalancingPlannerService

#### Chức năng
- Chạy **mỗi 12 giờ** tự động
- Phân tích GAP của tất cả depot
- Đề xuất 2 loại action:
  1. **RELOCATE**: Di chuyển xe từ depot thừa → depot thiếu
  2. **PURCHASE**: Mua thêm xe mới (nếu không đủ xe để relocate)

---

### 3.2. Thuật toán Matching (Ghép cặp)

#### Quy trình

```
Step 1: Tính GAP cho tất cả depot-model combinations
├─> Depot A - Tesla 3: GAP = -5 (THỪA 5 xe)
├─> Depot B - Tesla 3: GAP = +3 (THIẾU 3 xe)
└─> Depot C - Tesla 3: GAP = +8 (THIẾU 8 xe)

Step 2: Nhóm theo model (Tesla 3)
├─> Surpluses: [Depot A: -5]
└─> Shortages: [Depot B: +3, Depot C: +8]

Step 3: Ghép THỪA với THIẾU (theo priority)
├─> Depot C (thiếu 8, priority cao) gets first
│   └─> RELOCATE 5 xe từ Depot A → Depot C
│       ├─> Depot A: -5 + 5 = 0 (hết thừa)
│       └─> Depot C: +8 - 5 = +3 (còn thiếu 3)
│
├─> Depot C còn thiếu 3 xe
│   └─> PURCHASE 3 xe mới → Depot C
│
└─> Depot B (thiếu 3)
    └─> PURCHASE 3 xe mới → Depot B (không còn surplus)
```

#### Code implementation

```csharp
// File: RebalancingPlannerService.cs (Line 123-185)
foreach (var (vehicleType, gaps) in byVehicleType)
{
    var shortages = gaps
        .Where(g => g.Value.Gap > 0)
        .OrderByDescending(g => g.Value.Gap) // Ưu tiên thiếu nhiều
        .ToList();
    
    var surpluses = gaps
        .Where(g => g.Value.Gap < 0)
        .OrderBy(g => g.Value.Gap) // Ưu tiên thừa nhiều
        .ToList();

    foreach (var shortage in shortages)
    {
        var shortageGap = shortage.Value;
        var needed = shortageGap.Gap;

        // Bước 1: Thử relocate từ surplus trước
        foreach (var surplus in surpluses)
        {
            if (needed <= 0) break;

            var surplusGap = surplus.Value;
            var available = Math.Abs(surplusGap.Gap);

            if (available <= 0) continue;

            var relocateQty = Math.Min(needed, available);

            // Tạo RELOCATE plan
            plans.Add(new RebalancingPlan
            {
                PlanDate = DateTime.UtcNow.AddDays(1),
                FromDepotId = surplusGap.StationId,
                ToDepotId = shortageGap.StationId,
                ModelId = vehicleType,
                Quantity = relocateQty,
                ActionType = "RELOCATE",
                Priority = CalculatePriority(shortageGap.Gap, shortageGap.P90Demand),
                EstimatedCost = 0, // Chi phí di chuyển tối thiểu
                Status = "PROPOSED",
                Reason = $"Shortage of {shortageGap.Gap} units, " +
                         $"surplus of {available} at source"
            });

            needed -= relocateQty;           // Giảm nhu cầu
            surplusGap.Gap += relocateQty;  // Giảm thừa
        }

        // Bước 2: Nếu còn thiếu → PURCHASE
        if (needed > 0)
        {
            plans.Add(new RebalancingPlan
            {
                PlanDate = DateTime.UtcNow.AddDays(1),
                FromDepotId = null, // Không có depot nguồn
                ToDepotId = shortageGap.StationId,
                ModelId = vehicleType,
                Quantity = needed,
                ActionType = "PURCHASE",
                Priority = CalculatePriority(shortageGap.Gap, shortageGap.P90Demand),
                EstimatedCost = needed * 25000m, // $25k/xe
                Status = "PROPOSED",
                Reason = $"Cannot fulfill shortage through relocation"
            });
        }
    }
}
```

---

### 3.3. Ví dụ cụ thể End-to-End

#### Tình huống ban đầu

| Depot | Model | P90 Demand | Required | Current | GAP |
|-------|-------|------------|----------|---------|-----|
| Depot A | Tesla 3 | 8.0 | 6 | 10 | -4 (THỪA) |
| Depot B | Tesla 3 | 15.0 | 10 | 7 | +3 (THIẾU) |
| Depot C | Tesla 3 | 20.0 | 14 | 5 | +9 (THIẾU) |

#### Bước 1: Matching

```
Depot C (thiếu 9 xe, priority cao):
├─> Có thể relocate 4 xe từ Depot A
├─> Plan 1: RELOCATE 4 xe (A → C)
└─> Còn thiếu 5 xe
    └─> Plan 2: PURCHASE 5 xe mới → C

Depot B (thiếu 3 xe):
├─> Depot A đã hết thừa
└─> Plan 3: PURCHASE 3 xe mới → B
```

#### Kết quả trong bảng `RebalancingPlan`

| Id | PlanDate | FromDepotId | ToDepotId | ModelId | Qty | ActionType | Priority | Status |
|----|----------|-------------|-----------|---------|-----|------------|----------|--------|
| plan-1 | 2025-11-11 | depot-a | depot-c | tesla-3 | 4 | RELOCATE | 85 | PROPOSED |
| plan-2 | 2025-11-11 | NULL | depot-c | tesla-3 | 5 | PURCHASE | 90 | PROPOSED |
| plan-3 | 2025-11-11 | NULL | depot-b | tesla-3 | 3 | PURCHASE | 70 | PROPOSED |

---

## 🔄 LUỒNG HOẠT ĐỘNG END-TO-END

### Timeline hoạt động

```
00:00 ────────┐
              │
02:00         │ [MaterializedViewRefreshService]
              │ Refresh view mỗi 1 giờ
              │
04:00         │
              ├──> [DemandForecastGeneratorService]
06:00 ────────┤    Tạo dự báo 24h mỗi 6 giờ
              │    ├─> Query vw_rental_demand_30m_last_56d
08:00         │    ├─> Tính P90 cho từng slot
              │    └─> Insert DemandForecast (48 records)
10:00         │
              │
12:00 ────────┤──> [RebalancingPlannerService]
              │    Tạo kế hoạch điều chỉnh mỗi 12 giờ
14:00         │    ├─> GetStatsAsync() → P90
              │    ├─> GetRequiredUnits() → Required
16:00         │    ├─> Calculate GAP
              │    ├─> Match surplus/shortage
18:00         │    └─> Insert RebalancingPlan
              │
20:00         │
              │
22:00         │
              │
00:00 ────────┘ (Repeat)
```

---

### Luồng xử lý chi tiết

```
┌─────────────────────────────────────────────────────────────┐
│  1. MATERIALIZED VIEW REFRESH (Mỗi giờ)                     │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  vw_rental_demand_30m_last_56d                               │
│  - Group bookings by 30-min bins                             │
│  - Last 56 days history                                      │
│  - Exclude CANCELLED bookings                                │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  2. DEMAND FORECAST GENERATION (Mỗi 6 giờ)                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ For each (depot, model):                             │   │
│  │   ├─> Query historical demand from view             │   │
│  │   ├─> Calculate P90 per time slot                   │   │
│  │   └─> Generate 48 forecasts (24h × 2 slots)        │   │
│  └─────────────────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  DemandForecast Table                                        │
│  - ForecastTime, PredictedDemand (P90)                       │
│  - ConfidenceScore, Method = "P90"                           │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  3. CAPACITY ANALYSIS                                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ For each (depot, model):                             │   │
│  │   ├─> Get P90 from forecasts                        │   │
│  │   ├─> Calculate Required = ⌈P90×Trip/Cycle⌉        │   │
│  │   ├─> Get Current Available                         │   │
│  │   └─> GAP = Required - Current                      │   │
│  └─────────────────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  4. REBALANCING PLANNING (Mỗi 12 giờ)                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Group by vehicle type:                               │   │
│  │   ├─> Find SURPLUS depots (GAP < 0)                 │   │
│  │   ├─> Find SHORTAGE depots (GAP > 0)                │   │
│  │   │                                                   │   │
│  │   ├─> Match shortage with surplus:                  │   │
│  │   │   └─> Create RELOCATE plans                     │   │
│  │   │                                                   │   │
│  │   └─> Remaining shortage:                           │   │
│  │       └─> Create PURCHASE plans                     │   │
│  └─────────────────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  RebalancingPlan Table                                       │
│  - FromDepotId → ToDepotId                                   │
│  - ActionType: RELOCATE / PURCHASE                           │
│  - Status: PROPOSED → APPROVED → EXECUTED                   │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  5. ADMIN APPROVAL (Manual via API)                         │
│  - Admin reviews plans                                       │
│  - Approves or rejects                                       │
│  - Marks as EXECUTED when done                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 💾 CƠ SỞ DỮ LIỆU

### Bảng 1: `DemandForecast`

```sql
CREATE TABLE "DemandForecast" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "DepotId" VARCHAR(255) NOT NULL,
    "ModelId" VARCHAR(255) NOT NULL,
    "ForecastTime" TIMESTAMPTZ NOT NULL,
    "PredictedDemand" DECIMAL(10,2) NOT NULL,
    "ConfidenceScore" DECIMAL(5,2),
    "Method" VARCHAR(50),
    "HorizonMinutes" INT,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "IsDeleted" BOOLEAN DEFAULT FALSE,
    
    FOREIGN KEY ("DepotId") REFERENCES "Depot"("Id"),
    FOREIGN KEY ("ModelId") REFERENCES "Model"("Id")
);

CREATE INDEX "IX_DemandForecast_Time_Depot" 
    ON "DemandForecast"("ForecastTime", "DepotId");
```

**Ý nghĩa các trường**:
- `ForecastTime`: Thời điểm dự báo (rounded to 30-min)
- `PredictedDemand`: Số booking dự kiến (P90 value)
- `ConfidenceScore`: Độ tin cậy (0-100)
- `Method`: Phương pháp dự báo ("P90")
- `HorizonMinutes`: Độ dài khung giờ (30 phút)

---

### Bảng 2: `RebalancingPlan`

```sql
CREATE TABLE "RebalancingPlan" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "PlanDate" DATE NOT NULL,
    "FromDepotId" VARCHAR(255),           -- NULL nếu PURCHASE
    "ToDepotId" VARCHAR(255) NOT NULL,
    "ModelId" VARCHAR(255) NOT NULL,
    "Quantity" INT NOT NULL CHECK ("Quantity" > 0),
    "ActionType" VARCHAR(50) NOT NULL,    -- RELOCATE, PURCHASE
    "Priority" INT DEFAULT 0,             -- 0-100
    "EstimatedCost" DECIMAL(18,2),
    "Status" VARCHAR(50) DEFAULT 'PROPOSED',
    "Reason" TEXT,
    "ApprovedAt" TIMESTAMPTZ,
    "ApprovedBy" VARCHAR(255),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "CreatedBy" VARCHAR(255),
    "IsDeleted" BOOLEAN DEFAULT FALSE,
    
    FOREIGN KEY ("FromDepotId") REFERENCES "Depot"("Id"),
    FOREIGN KEY ("ToDepotId") REFERENCES "Depot"("Id"),
    FOREIGN KEY ("ModelId") REFERENCES "Model"("Id")
);

CREATE INDEX "IX_RebalancingPlan_PlanDate_Status" 
    ON "RebalancingPlan"("PlanDate", "Status");
```

**Ý nghĩa các trường**:
- `FromDepotId`: Depot nguồn (NULL nếu mua mới)
- `ToDepotId`: Depot đích
- `ActionType`: RELOCATE (di chuyển) hoặc PURCHASE (mua mới)
- `Priority`: Độ ưu tiên (0=low, 100=urgent)
- `Status`: PROPOSED → APPROVED → EXECUTED → CANCELLED

---

### View: `vw_rental_demand_30m_last_56d`

```sql
CREATE MATERIALIZED VIEW vw_rental_demand_30m_last_56d AS
SELECT 
    ob."DepotId" AS station_id,
    ob."ModelId" AS vehicle_type,
    DATE_BIN('30 minutes'::INTERVAL, ob."StartAt", 
        '1970-01-01 00:00:00'::TIMESTAMP) AS bin_ts,
    COUNT(*) AS demand
FROM "OrderBooking" ob
WHERE ob."StartAt" >= NOW() - INTERVAL '56 days'
  AND ob."Status" NOT IN ('CANCELLED', 'REFUND_PENDING')
  AND NOT ob."IsDeleted"
GROUP BY ob."DepotId", ob."ModelId", bin_ts;

-- Index để tăng tốc query
CREATE INDEX idx_rental_demand_station_vehicle_time
    ON vw_rental_demand_30m_last_56d(station_id, vehicle_type, bin_ts);
```

**Refresh**:
```sql
REFRESH MATERIALIZED VIEW CONCURRENTLY vw_rental_demand_30m_last_56d;
```

---

## 🌐 API ENDPOINTS

### 1. GET /api/forecasting/demand-stats

**Mục đích**: Lấy thống kê nhu cầu (Mean, P90) theo slot

**Query Parameters**:
- `stationIds[]`: Danh sách depot IDs (optional)
- `vehicleTypes[]`: Danh sách model IDs (optional)

**Response**:
```json
{
  "stats": {
    "depot-001_tesla-3_1_8_0": {
      "stationId": "depot-001",
      "vehicleType": "tesla-3",
      "dayOfWeek": 1,
      "hour": 8,
      "minute": 0,
      "mean": 10.2,
      "p90": 12.5
    }
  }
}
```

---

### 2. GET /api/forecasting/capacity-recommendations

**Mục đích**: Lấy danh sách depot cần điều chỉnh

**Query Parameters**:
- `avgTripHours`: Thời gian thuê TB (default: 2.0)
- `turnaroundHours`: Thời gian vệ sinh (default: 1.0)

**Response**:
```json
{
  "recommendations": [
    {
      "stationId": "depot-001",
      "stationName": "Depot Thủ Đức",
      "vehicleTypeId": "tesla-model-3",
      "vehicleTypeName": "Tesla Model 3",
      "peakP90Demand": 12.5,
      "peakSlot": {
        "dayOfWeek": 1,
        "hour": 8,
        "minute": 0
      },
      "requiredUnits": 8,
      "currentAvailablePeak24h": 5,
      "gap": 3,
      "priority": 65,
      "recommendedAction": "RELOCATE or PURCHASE",
      "reason": "Gap of 3 units during peak demand"
    }
  ]
}
```

---

### 3. GET /api/rebalancing-plans

**Mục đích**: Lấy danh sách kế hoạch điều chỉnh

**Query Parameters**:
- `planDate`: Ngày kế hoạch (yyyy-MM-dd)
- `status`: PROPOSED / APPROVED / EXECUTED (optional)
- `actionType`: RELOCATE / PURCHASE (optional)

**Response**:
```json
{
  "plans": [
    {
      "id": "plan-12345",
      "planDate": "2025-11-11",
      "fromDepot": {
        "id": "depot-002",
        "name": "Depot Quận 1"
      },
      "toDepot": {
        "id": "depot-001",
        "name": "Depot Thủ Đức"
      },
      "vehicleType": {
        "id": "tesla-model-3",
        "name": "Tesla Model 3"
      },
      "quantity": 4,
      "actionType": "RELOCATE",
      "priority": 85,
      "estimatedCost": 0,
      "status": "PROPOSED",
      "reason": "Shortage of 8 units, surplus of 5 at source",
      "createdAt": "2025-11-10T12:00:00Z"
    }
  ],
  "summary": {
    "totalPlans": 5,
    "relocatePlans": 2,
    "purchasePlans": 3,
    "totalEstimatedCost": 200000
  }
}
```

---

### 4. POST /api/rebalancing-plans/{id}/approve

**Mục đích**: Admin phê duyệt kế hoạch

**Request Body**:
```json
{
  "adminNote": "Approved for execution"
}
```

**Response**:
```json
{
  "id": "plan-12345",
  "status": "APPROVED",
  "approvedAt": "2025-11-10T14:30:00Z",
  "approvedBy": "admin-001"
}
```

---

### 5. POST /api/rebalancing-plans/{id}/execute

**Mục đích**: Đánh dấu kế hoạch đã thực hiện

**Response**:
```json
{
  "id": "plan-12345",
  "status": "EXECUTED",
  "executedAt": "2025-11-11T08:00:00Z"
}
```

---

## 📊 DASHBOARD & METRICS

### Metrics quan trọng

1. **Forecast Accuracy**: So sánh dự báo vs thực tế
2. **GAP Distribution**: Phân bố thiếu/thừa xe
3. **Action Effectiveness**: Tỷ lệ thành công của plans
4. **Cost Savings**: Tiết kiệm từ RELOCATE thay vì PURCHASE

### Dashboard Views

```
┌─────────────────────────────────────────────────────────────┐
│                    DEMAND FORECAST DASHBOARD                 │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  📈 Peak Demand by Depot (Next 24h)                          │
│  ┌─────────┬──────────┬─────────┬──────────┐               │
│  │ Depot   │ Model    │ P90     │ Required │               │
│  ├─────────┼──────────┼─────────┼──────────┤               │
│  │ TD      │ Tesla 3  │  12.5   │    8     │               │
│  │ Q1      │ VF8      │   8.2   │    5     │               │
│  └─────────┴──────────┴─────────┴──────────┘               │
│                                                               │
│  🚗 Capacity Status                                           │
│  ┌─────────────────────────────────────────┐                │
│  │ Depot TD - Tesla 3:   █████░░░░░ 55%    │                │
│  │ Depot Q1 - VF8:       ███████░░░ 70%    │                │
│  └─────────────────────────────────────────┘                │
│                                                               │
│  ⚠️  Action Required                                          │
│  • RELOCATE 4 Tesla 3: Q1 → TD (Priority 85)                │
│  • PURCHASE 3 VF8 → Q7 (Priority 90)                         │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎓 TÓM TẮT KEY CONCEPTS

### 1. P90 (Percentile 90)
- Giá trị mà 90% trường hợp ≤ giá trị đó
- An toàn hơn Mean (không bị outliers ảnh hưởng)
- Cân bằng giữa đáp ứng nhu cầu và chi phí

### 2. Required Units Formula
```
Required = ⌈ P90 × Trip Time / (Trip + Turnaround) ⌉
```
- Tính số xe cần để đáp ứng nhu cầu đỉnh
- Tính đến thời gian vệ sinh/bảo trì

### 3. GAP Analysis
```
GAP = Required - Current
```
- GAP > 0: THIẾU xe (shortage)
- GAP < 0: THỪA xe (surplus)
- Cơ sở để matching và planning

### 4. Rebalancing Strategy
- **Ưu tiên 1**: RELOCATE (di chuyển từ thừa → thiếu)
- **Ưu tiên 2**: PURCHASE (mua mới nếu không đủ)
- Mục tiêu: Minimize cost, maximize coverage

---

## 🔧 VẬN HÀNH & BẢO TRÌ

### Monitoring

1. **Service Health**:
   - Check logs của 3 background services
   - Alert nếu service fail > 2 lần

2. **Data Quality**:
   - Verify materialized view refresh
   - Check forecast coverage (phải có cho tất cả depots)

3. **Plan Execution**:
   - Track approval rate
   - Monitor execution delays

### Troubleshooting

**Vấn đề**: Forecast không chính xác
- **Nguyên nhân**: Ít dữ liệu lịch sử (<7 ngày)
- **Giải pháp**: Đợi thu thập thêm data hoặc giảm confidence threshold

**Vấn đề**: Quá nhiều PURCHASE plans
- **Nguyên nhân**: Không có depot thừa để relocate
- **Giải pháp**: Review demand forecasts, có thể điều chỉnh avgTripHours

---

## 📚 TÀI LIỆU THAM KHẢO

- **RFC Document**: `/docs/rfc-forecast-capacity.md`
- **Implementation Summary**: `/docs/README-demand-forecasting.md`
- **Service Code**: 
  - `EVSRS.Services/Service/ForecastingService.cs`
  - `EVSRS.API/Services/DemandForecastGeneratorService.cs`
  - `EVSRS.API/Services/RebalancingPlannerService.cs`

---

**Tác giả**: System Documentation  
**Ngày cập nhật**: 2025-11-10  
**Version**: 1.0
