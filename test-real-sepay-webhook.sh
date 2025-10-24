#!/bin/bash

# Test with the actual SePay payload from the log
API_URL=${1:-"http://localhost:8080"}
SEPAY_API_KEY="2AFQK57BVHWOSGPEETPZA0PCBJ7TIZM9FAFIWU1NJVU3ULSIYDMXHQH4VBD6LGNW"

echo "Testing with actual SePay webhook payload..."
echo "API URL: $API_URL"

# Real payload from the log you provided
REAL_PAYLOAD='{
  "gateway":"VPBank",
  "transactionDate":"2025-10-24 22:23:00",
  "accountNumber":"0976247994",
  "subAccount":null,
  "code":null,
  "content":"NHAN TU 9021166368134 TRACE 235920 ND TF6654849BK202510247208",
  "transferType":"in",
  "description":"BankAPINotify NHAN TU 9021166368134 TRACE 235920 ND TF6654849BK202510247208",
  "transferAmount":17100,
  "referenceCode":"FT25298449278802",
  "accumulated":0,
  "id":27560241
}'

echo ""
echo "=== Testing with real SePay payload ==="
curl -X POST "$API_URL/api/sepay-auth/hooks/payment" \
  -H "Content-Type: application/json" \
  -H "Authorization: Apikey $SEPAY_API_KEY" \
  -d "$REAL_PAYLOAD" \
  -v

echo ""
echo "Test completed!"