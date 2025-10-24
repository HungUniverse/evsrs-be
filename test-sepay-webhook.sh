#!/bin/bash

# Test script for SePay webhook
# Usage: ./test-sepay-webhook.sh [url]

API_URL=${1:-"http://localhost:8080"}
SEPAY_API_KEY="2AFQK57BVHWOSGPEETPZA0PCBJ7TIZM9FAFIWU1NJVU3ULSIYDMXHQH4VBD6LGNW"

echo "Testing SePay webhook integration..."
echo "API URL: $API_URL"
echo "Using API Key: ${SEPAY_API_KEY:0:10}..."

# Test payload - adjust the content to match your order format
TEST_PAYLOAD='{
  "content": "ORD1234567 - Payment received",
  "amount": 100000,
  "description": "Test payment",
  "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)'"
}'

echo ""
echo "=== Test 1: Standard Apikey format ==="
curl -X POST "$API_URL/api/sepay-auth/hooks/payment" \
  -H "Content-Type: application/json" \
  -H "Authorization: Apikey $SEPAY_API_KEY" \
  -d "$TEST_PAYLOAD" \
  -v

echo ""
echo ""
echo "=== Test 2: Alternative ApiKey format ==="
curl -X POST "$API_URL/api/sepay-auth/hooks/payment" \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey $SEPAY_API_KEY" \
  -d "$TEST_PAYLOAD" \
  -v

echo ""
echo ""
echo "=== Test 3: Apikey header (not Authorization) ==="
curl -X POST "$API_URL/api/sepay-auth/hooks/payment" \
  -H "Content-Type: application/json" \
  -H "Apikey: $SEPAY_API_KEY" \
  -d "$TEST_PAYLOAD" \
  -v

echo ""
echo ""
echo "=== Test 4: API-Key header ==="
curl -X POST "$API_URL/api/sepay-auth/hooks/payment" \
  -H "Content-Type: application/json" \
  -H "API-Key: $SEPAY_API_KEY" \
  -d "$TEST_PAYLOAD" \
  -v

echo ""
echo ""
echo "=== Test 5: Just the key value ==="
curl -X POST "$API_URL/api/sepay-auth/hooks/payment" \
  -H "Content-Type: application/json" \
  -H "Authorization: $SEPAY_API_KEY" \
  -d "$TEST_PAYLOAD" \
  -v

echo ""
echo "Tests completed!"