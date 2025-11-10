# API Forecast - Hướng dẫn sử dụng

## Tổng quan
API Forecast dự báo nhu cầu xe điện và đề xuất số lượng xe cần mua/điều chỉnh cho mỗi depot.

## Endpoint

### GET /api/forecast/{stationId}

Lấy thống kê nhu cầu và đề xuất capacity cho một depot.

**Parameters:**
- `stationId` (path, required): ID của depot
- `vehicleType` (query, optional): Lọc theo loại xe (Model ID)
- `horizonDays` (query, default=7): Số ngày quá khứ để phân tích
- `avgTripHours` (query, default=2.0): Thời gian thuê trung bình (giờ)
- `turnaroundHours` (query, default=1.0): Thời gian chuẩn bị xe (giờ)

**Headers:**
```
Authorization: Bearer {your-jwt-token}
```

**Response 200 OK:**
```json
{
  "dateRange": {
    "startDate": "2025-11-03T00:00:00Z",
    "endDate": "2025-11-10T00:00:00Z"
  },
  "recommendations": [
    {
      "stationId": "depot-uuid-123",
      "vehicleType": "model-uuid-456",
      "peakP90Demand": 15.0,
      "requiredUnits": 5,
      "currentAvailablePeak24h": 3,
      "gap": 2,
      "recommendedAction": "BUY",
      "priority": 67
    }
  ]
}
```

**Response 404:**
```json
{
  "error": "Forecast feature is not enabled"
}
```
hoặc
```json
{
  "error": "No data found for station {stationId}"
}
```

## Cách hoạt động

### Bước 1: Thu thập dữ liệu
API query materialized view `vw_rental_demand_30m_last_56d` để lấy:
- Số lượng booking trong mỗi time slot 30 phút
- Nhóm theo depot và loại xe
- **Dữ liệu 24 giờ gần nhất** (đã rút ngắn từ 56 ngày)

### Bước 2: Tính toán thống kê
- **P90 Demand**: Giá trị percentile 90 của nhu cầu (số xe cần trong peak time)
- **Mean**: Trung bình số xe thuê
- **Median (P50)**: Trung vị

### Bước 3: Tính Required Units
```
requiredUnits = CEILING(P90 * (avgTripHours + turnaroundHours) / 24)
```
Công thức này tính số xe cần dựa trên:
- P90 demand (peak nhu cầu)
- Thời gian thuê + chuẩn bị (3 giờ mặc định)
- Số giờ trong ngày (24h)

### Bước 4: Tính Gap
```
gap = requiredUnits - currentAvailablePeak24h
```
- Nếu gap > 0: Thiếu xe → **BUY** (mua thêm)
- Nếu gap < 0: Thừa xe → **SURPLUS** (dư thừa)
- Nếu gap = 0: Đủ xe → **OK**

### Bước 5: Tính Priority
```
priority = (gap / P90) * 100
```
Priority càng cao = càng cần xử lý gấp

## Ví dụ sử dụng

### 1. Lấy forecast cho tất cả xe tại depot
```bash
GET /api/forecast/1ca81b37-db00-42c5-b740-92575026fcc9?horizonDays=7
```

### 2. Lấy forecast cho 1 loại xe cụ thể
```bash
GET /api/forecast/1ca81b37-db00-42c5-b740-92575026fcc9?vehicleType=model-123&horizonDays=14
```

### 3. Điều chỉnh tham số tính toán
```bash
GET /api/forecast/1ca81b37-db00-42c5-b740-92575026fcc9?avgTripHours=3.0&turnaroundHours=0.5
```

## Yêu cầu dữ liệu

API cần **ít nhất 20-30 OrderBooking** trong 24 giờ qua để tính toán chính xác:

1. **OrderBooking** phải có:
   - `StartAt` trong khoảng NOW() - 1 day (24 giờ qua)
   - `IsDeleted = FALSE`
   - `CarEVDetailId` hợp lệ (liên kết với CarEV)

2. **CarEV** phải có:
   - `ModelId` hợp lệ (liên kết với Model)
   - `DepotId` hợp lệ (liên kết với Depot)
   - `IsDeleted = FALSE`

3. **Materialized View** cần được refresh:
   ```sql
   REFRESH MATERIALIZED VIEW vw_rental_demand_30m_last_56d;
   ```

## Troubleshooting

### Lỗi: "No data found for station"

**Nguyên nhân:**
- Materialized view trống (không có dữ liệu)
- OrderBooking không có trong 24 giờ qua
- Depot ID không tồn tại

**Giải pháp:**
1. Chạy `debug-forecast.sql` để kiểm tra
2. Tạo OrderBooking test bằng `seed-forecast-data.sql`
3. Refresh view: `REFRESH MATERIALIZED VIEW vw_rental_demand_30m_last_56d;`

### Lỗi: "Forecast feature is not enabled"

**Nguyên nhân:**
Feature flag bị tắt trong `appsettings.json`

**Giải pháp:**
```json
{
  "Features": {
    "ForecastCapacity": true
  }
}
```

### Lỗi 401 Unauthorized

**Nguyên nhân:**
Thiếu JWT token hoặc token hết hạn

**Giải pháp:**
Thêm header: `Authorization: Bearer {your-token}`

## Lưu ý quan trọng

⚠️ **Materialized View không tự động refresh!**

Sau khi thêm OrderBooking mới, bạn PHẢI chạy:
```sql
REFRESH MATERIALIZED VIEW vw_rental_demand_30m_last_56d;
```

Hoặc setup cron job tự động refresh mỗi giờ:
```sql
-- PostgreSQL pg_cron extension
SELECT cron.schedule('refresh-demand-view', '0 * * * *', 
  'REFRESH MATERIALIZED VIEW vw_rental_demand_30m_last_56d');
```
