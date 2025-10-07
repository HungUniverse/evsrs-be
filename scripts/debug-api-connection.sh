#!/bin/bash

# Debug API Connection Script
# This script helps debug API database connection issues

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔍 API Database Connection Debug${NC}"
echo ""

# Load environment variables
if [ -f ".env" ]; then
    source .env
else
    echo -e "${RED}❌ .env file not found${NC}"
    exit 1
fi

echo -e "${YELLOW}📊 Environment Variables:${NC}"
echo "POSTGRES_HOST: ${POSTGRES_HOST}"
echo "POSTGRES_PORT: ${POSTGRES_PORT}"
echo "POSTGRES_DB: ${POSTGRES_DB}"
echo "POSTGRES_USER: ${POSTGRES_USER}"
echo "POSTGRES_SSL_MODE: ${POSTGRES_SSL_MODE}"
echo ""

# Build connection strings
PSQL_URL="postgresql://${POSTGRES_USER}:${POSTGRES_PASSWORD}@${POSTGRES_HOST}:${POSTGRES_PORT}/${POSTGRES_DB}?sslmode=require"
DOTNET_CONNECTION="Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Pooling=true;MinPoolSize=5;MaxPoolSize=20;Timeout=15;Encoding=UTF8;SslMode=${POSTGRES_SSL_MODE};"

echo -e "${YELLOW}🔗 Connection Strings:${NC}"
echo "PostgreSQL URL: ${PSQL_URL}"
echo "ASP.NET Core: ${DOTNET_CONNECTION}"
echo ""

echo -e "${YELLOW}1️⃣ Testing direct PostgreSQL connection...${NC}"
if docker run --rm postgres:15-alpine psql "$PSQL_URL" -c "SELECT 'Direct connection OK' as status, version();" 2>/dev/null; then
    echo -e "${GREEN}✅ Direct PostgreSQL connection successful${NC}"
else
    echo -e "${RED}❌ Direct PostgreSQL connection failed${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}2️⃣ Testing from API container environment...${NC}"

# Create a temporary test container with the same environment as API
docker run --rm \
    --env-file .env \
    -e ConnectionStrings__DefaultConnection="$DOTNET_CONNECTION" \
    postgres:15-alpine \
    psql "$PSQL_URL" -c "
        SELECT 'API environment test OK' as status;
        SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' LIMIT 5;
    " 2>/dev/null

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ API environment connection successful${NC}"
else
    echo -e "${RED}❌ API environment connection failed${NC}"
fi

echo ""
echo -e "${YELLOW}3️⃣ Testing essential tables...${NC}"

TABLES=("User" "CarEV" "Depot" "OrderBooking")

for table in "${TABLES[@]}"; do
    COUNT=$(docker run --rm postgres:15-alpine psql "$PSQL_URL" -t -c "SELECT COUNT(*) FROM \"$table\";" 2>/dev/null | tr -d ' ')
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Table '$table': $COUNT rows${NC}"
    else
        echo -e "${RED}❌ Table '$table': not accessible${NC}"
    fi
done

echo ""
echo -e "${YELLOW}4️⃣ Checking current API container (if running)...${NC}"

if docker ps --format "table {{.Names}}" | grep -q "evsrs-api"; then
    echo -e "${BLUE}API container is running. Checking logs...${NC}"
    echo -e "${YELLOW}Last 10 lines of API logs:${NC}"
    docker logs evsrs-api --tail=10 2>/dev/null || echo "Could not fetch logs"
    
    echo ""
    echo -e "${YELLOW}Testing API health endpoint...${NC}"
    if curl -f http://localhost:8080/health > /dev/null 2>&1; then
        echo -e "${GREEN}✅ API health endpoint responding${NC}"
    else
        echo -e "${RED}❌ API health endpoint not responding${NC}"
    fi
    
    echo ""
    echo -e "${YELLOW}API container environment:${NC}"
    docker exec evsrs-api env | grep -E "(POSTGRES|ConnectionStrings)" 2>/dev/null || echo "Could not access container environment"
    
else
    echo -e "${YELLOW}ℹ️ API container is not running${NC}"
fi

echo ""
echo -e "${YELLOW}5️⃣ Network connectivity test...${NC}"

# Test if we can reach the DigitalOcean host
if ping -c 1 ${POSTGRES_HOST} > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Can ping DigitalOcean database host${NC}"
else
    echo -e "${RED}❌ Cannot ping DigitalOcean database host${NC}"
fi

# Test port connectivity
if nc -z ${POSTGRES_HOST} ${POSTGRES_PORT} 2>/dev/null; then
    echo -e "${GREEN}✅ Port ${POSTGRES_PORT} is reachable${NC}"
else
    echo -e "${RED}❌ Port ${POSTGRES_PORT} is not reachable${NC}"
fi

echo ""
echo -e "${GREEN}🎯 Debug completed!${NC}"
echo ""
echo -e "${BLUE}📋 To fix API connection issues:${NC}"
echo "1. Run cleanup script: ./scripts/cleanup-vps-postgres.sh"
echo "2. Rebuild API: docker-compose -f docker-compose.vps.yml build --no-cache evsrs-api"
echo "3. Start clean: docker-compose -f docker-compose.vps.yml up -d"
echo "4. Check logs: docker-compose -f docker-compose.vps.yml logs -f evsrs-api"