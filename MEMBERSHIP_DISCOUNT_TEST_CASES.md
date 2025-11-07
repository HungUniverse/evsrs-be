# 🎯 Membership Discount Logic - Test Cases

## 📋 Logic Overview

### **Khi khách hàng đặt đơn:**
1. Hệ thống tính giá gốc = `dailyPrice * coefficient`
2. Áp dụng model discount (Sale %) nếu có
3. **✅ ÁP DỤNG MEMBERSHIP DISCOUNT** dựa trên hạng hiện tại của user
4. Tính deposit & remaining amount

### **Membership Levels & Discounts:**
```
None:   TotalBill < 20,000    → 0% discount
Bronze: TotalBill ≥ 20,000    → 10% discount
Silver: TotalBill ≥ 50,000    → 20% discount
Gold:   TotalBill ≥ 100,000   → 30% discount
```

---

## 🧪 Test Scenarios

### **Scenario 1: User mới (None level)**
```
User: UserA (TotalOrderBill = 0)
Membership: None (0% discount)

Order 1:
- Car: Tesla Model 3 (Price: 50,000 VND/day)
- Time: 6:00 - 22:00 (1 full day)
- Coefficient: 1.0
- Model Sale: 0%

Calculation:
- Base Price: 50,000 * 1.0 = 50,000
- Model Discount: 0%
- Membership Discount: 0% (None level)
- Final Price: 50,000 VND ✅

After Complete:
- TotalOrderBill = 50,000
- Upgrade to: Silver (≥ 50k) 🎉
```

---

### **Scenario 2: User có Bronze level**
```
User: UserB (TotalOrderBill = 25,000)
Membership: Bronze (10% discount)

Order 2:
- Car: VinFast VF8 (Price: 45,000 VND/day)
- Time: 6:00 - 12:30 (morning shift)
- Coefficient: 0.4
- Model Sale: 10%

Calculation:
- Base Price: 45,000 * 0.4 = 18,000
- Model Discount: 18,000 * 10% = 1,800 → 16,200
- Membership Discount: 16,200 * 10% = 1,620
- Final Price: 14,580 VND ✅

After Complete:
- TotalOrderBill = 25,000 + 14,580 = 39,580
- Still Bronze (< 50k)
```

---

### **Scenario 3: User có Silver level**
```
User: UserC (TotalOrderBill = 65,000)
Membership: Silver (20% discount)

Order 3:
- Car: BMW IX3 (Price: 55,000 VND/day)
- Time: 12:30 - 22:00 (afternoon shift)
- Coefficient: 0.6
- Model Sale: 8%

Calculation:
- Base Price: 55,000 * 0.6 = 33,000
- Model Discount: 33,000 * 8% = 2,640 → 30,360
- Membership Discount: 30,360 * 20% = 6,072
- Final Price: 24,288 VND ✅

After Complete:
- TotalOrderBill = 65,000 + 24,288 = 89,288
- Still Silver (< 100k)
```

---

### **Scenario 4: User có Gold level**
```
User: UserD (TotalOrderBill = 150,000)
Membership: Gold (30% discount)

Order 4:
- Car: Mercedes GLA (Price: 55,000 VND/day)
- Time: 6:00 - 22:00 (2 full days)
- Coefficient: 2.0
- Model Sale: 12%

Calculation:
- Base Price: 55,000 * 2.0 = 110,000
- Model Discount: 110,000 * 12% = 13,200 → 96,800
- Membership Discount: 96,800 * 30% = 29,040
- Final Price: 67,760 VND ✅

After Complete:
- TotalOrderBill = 150,000 + 67,760 = 217,760
- Still Gold ⭐
```

---

### **Scenario 5: User upgrade sau order**
```
User: UserE (TotalOrderBill = 18,000)
Membership: None (0% discount)

Order 5:
- Car: VinFast VF3 (Price: 30,000 VND/day)
- Time: 6:00 - 12:30 (morning)
- Coefficient: 0.4
- Model Sale: 10%

Calculation:
- Base Price: 30,000 * 0.4 = 12,000
- Model Discount: 12,000 * 10% = 1,200 → 10,800
- Membership Discount: 0% (None level)
- Final Price: 10,800 VND ✅

After Complete:
- TotalOrderBill = 18,000 + 10,800 = 28,800
- Upgrade to: Bronze (≥ 20k) 🎉

Order 6 (NGAY SAU ĐÓ):
- Car: Same VF3
- Same conditions
- Membership: Bronze (10% discount) ← ÁP DỤNG DISCOUNT MỚI!

Calculation:
- Base Price: 12,000
- Model Discount: 10,800
- Membership Discount: 10,800 * 10% = 1,080
- Final Price: 9,720 VND ✅ (Rẻ hơn 1,080!)
```

---

## 🔍 API Flow

### **1. Create Booking (User)**
```http
POST /api/order-booking
Authorization: Bearer {userToken}
Content-Type: application/json

{
  "carEVDetailId": "car-id",
  "startAt": "2025-11-07T06:00:00Z",
  "endAt": "2025-11-07T22:00:00Z",
  "paymentType": "FULL"
}
```

**Backend Process:**
```csharp
1. Get user's current membership
2. Calculate base cost with coefficient
3. Apply model sale discount
4. ✅ Apply membership discount (10%, 20%, 30%)
5. Return final price with discount applied
```

---

### **2. Complete Booking (Staff)**
```http
POST /api/order-booking/{id}/complete
Authorization: Bearer {staffToken}
```

**Backend Process:**
```csharp
1. Mark order as COMPLETE
2. Add DepositAmount to TotalOrderBill
3. Check upgrade conditions:
   - If TotalBill ≥ 100k → Gold
   - Else if TotalBill ≥ 50k → Silver
   - Else if TotalBill ≥ 20k → Bronze
4. Update membership level
5. Log upgrade if happened
```

---

## ✅ Expected Results

### **For None Users:**
- Pay full price (no membership discount)
- Get discount on next order after reaching 20k

### **For Bronze Users:**
- Save 10% on every order
- Upgrade to Silver after reaching 50k total

### **For Silver Users:**
- Save 20% on every order
- Upgrade to Gold after reaching 100k total

### **For Gold Users:**
- Save 30% on every order permanently 🎉
- Maximum benefit!

---

## 📊 Database State

### **After Setup:**
```sql
-- MembershipConfig table
| Level  | DiscountPercent | RequiredAmount |
|--------|----------------|----------------|
| None   | 0              | 0              |
| Bronze | 10             | 20000          |
| Silver | 20             | 50000          |
| Gold   | 30             | 100000         |
```

### **User Membership Example:**
```sql
-- User với Silver level
| UserId | MembershipConfigId | TotalOrderBill |
|--------|-------------------|----------------|
| user-1 | silver-config-id  | 65000          |

-- Khi user-1 đặt đơn mới:
-- → Áp dụng 20% discount
-- → Sau khi complete, TotalOrderBill tăng lên
-- → Nếu đạt 100k thì auto upgrade to Gold
```

---

## 🎓 Admin Management

Admin có thể chỉnh sửa:
- `DiscountPercent` (0-100%)
- `RequiredAmount` (ngưỡng nâng hạng)

```http
PUT /api/membership-config/{id}
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "discountPercent": 15,  // Tăng Bronze từ 10% → 15%
  "requiredAmount": 25000 // Tăng threshold từ 20k → 25k
}
```

**Effect:**
- All Bronze users sẽ được giảm 15% cho đơn mới
- User cần 25k để lên Bronze (instead of 20k)

---

## 🚀 Testing Commands

```bash
# 1. Build project
dotnet build

# 2. Run API
dotnet run --project EVSRS.API

# 3. Test create booking với membership discount
# → Check console log: "💎 Applied X% membership discount"

# 4. Test complete booking
# → Check console log: "🎉 User upgraded to {Level}"
```
