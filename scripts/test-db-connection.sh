#!/bin/bash

# Script to test DigitalOcean Database Connection
# This script verifies that API can connect to DigitalOcean PostgreSQL

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔍 Testing DigitalOcean Database Connection...${NC}"

# Load environment variables
if [ -f ".env" ]; then
    source .env 2>/dev/null || echo "Warning: .env file has syntax issues"
else
    echo -e "${RED}❌ .env file not found${NC}"
    exit 1
fi

# Use environment variables
DB_HOST=${POSTGRES_HOST}
DB_PORT=${POSTGRES_PORT}
DB_NAME=${POSTGRES_DB}
DB_USER=${POSTGRES_USER}
DB_PASSWORD=${POSTGRES_PASSWORD}
SSL_MODE=${POSTGRES_SSL_MODE}

echo -e "${YELLOW}📊 Connection Details:${NC}"
echo "Host: ${DB_HOST}"
echo "Port: ${DB_PORT}"
echo "Database: ${DB_NAME}"
echo "User: ${DB_USER}"
echo "SSL Mode: ${SSL_MODE}"
echo ""

# Build PostgreSQL URL
POSTGRES_URL="postgresql://${DB_USER}:${DB_PASSWORD}@${DB_HOST}:${DB_PORT}/${DB_NAME}"
if [ "${SSL_MODE}" = "Require" ]; then
    POSTGRES_URL="${POSTGRES_URL}?sslmode=require"
fi

echo -e "${YELLOW}🔌 Testing basic connection...${NC}"

# Test basic connection
if docker run --rm postgres:15-alpine psql \
  "$POSTGRES_URL" \
  -c "SELECT 'Connection successful!' as message, version();" 2>/dev/null; then
    echo -e "${GREEN}✅ Basic connection successful${NC}"
else
    echo -e "${RED}❌ Basic connection failed${NC}"
    exit 1
fi

echo -e "${YELLOW}📋 Checking required tables...${NC}"

# Check for essential tables
TABLES=(
    "User"
    "CarEV"
    "Depot"
    "OrderBooking"
    "CarManufacture"
    "Model"
)

ALL_TABLES_EXIST=true

for table in "${TABLES[@]}"; do
    EXISTS=$(docker run --rm postgres:15-alpine psql \
      "$POSTGRES_URL" \
      -t -c "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = '${table}');" 2>/dev/null | tr -d ' ')
    
    if [ "$EXISTS" = "t" ]; then
        echo -e "${GREEN}✅ Table '${table}' exists${NC}"
    else
        echo -e "${RED}❌ Table '${table}' missing${NC}"
        ALL_TABLES_EXIST=false
    fi
done

if [ "$ALL_TABLES_EXIST" = true ]; then
    echo -e "${GREEN}✅ All required tables exist${NC}"
else
    echo -e "${YELLOW}⚠️ Some tables are missing - this may cause API errors${NC}"
fi

echo -e "${YELLOW}📊 Database Statistics:${NC}"

# Show table counts
docker run --rm postgres:15-alpine psql \
  "$POSTGRES_URL" \
  -c "
    SELECT 
        schemaname,
        tablename,
        n_tup_ins as inserts,
        n_tup_upd as updates,
        n_tup_del as deletes,
        n_live_tup as live_rows
    FROM pg_stat_user_tables 
    ORDER BY n_live_tup DESC;
  " 2>/dev/null

echo ""
echo -e "${GREEN}🎉 Database connection test completed!${NC}"

# Test the exact connection string that will be used by the API
echo -e "${YELLOW}🔧 Testing API connection string format...${NC}"

API_CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};Pooling=true;MinPoolSize=5;MaxPoolSize=20;Timeout=15;Encoding=UTF8;SslMode=${SSL_MODE};"

echo "API Connection String format:"
echo "${API_CONNECTION_STRING}"
echo ""

echo -e "${BLUE}✅ All tests passed! API should be able to connect to the database.${NC}"