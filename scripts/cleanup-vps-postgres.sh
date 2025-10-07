#!/bin/bash

# VPS PostgreSQL Cleanup Script
# This script completely removes local PostgreSQL containers and ensures API uses DigitalOcean database only

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🧹 VPS PostgreSQL Cleanup Script${NC}"
echo -e "${YELLOW}This will remove ALL local PostgreSQL containers and data${NC}"
echo -e "${YELLOW}API will use DigitalOcean database exclusively${NC}"
echo ""

# Confirmation
read -p "Are you sure you want to continue? [y/N]: " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cleanup cancelled."
    exit 0
fi

echo -e "${YELLOW}🛑 Step 1: Stopping all containers...${NC}"
docker-compose -f docker-compose.vps.yml down 2>/dev/null || true
docker stop $(docker ps -aq) 2>/dev/null || true

echo -e "${YELLOW}🗑️ Step 2: Removing PostgreSQL containers...${NC}"
# Remove containers with postgres in name
docker rm -f $(docker ps -aq --filter "name=postgres") 2>/dev/null || true
docker rm -f $(docker ps -aq --filter "name=evsrs-postgres") 2>/dev/null || true
docker rm -f $(docker ps -aq --filter "ancestor=postgres") 2>/dev/null || true

echo -e "${YELLOW}📦 Step 3: Removing PostgreSQL images...${NC}"
docker rmi $(docker images postgres -q) 2>/dev/null || true

echo -e "${YELLOW}💾 Step 4: Removing PostgreSQL volumes...${NC}"
docker volume rm $(docker volume ls -q --filter "name=postgres") 2>/dev/null || true
docker volume rm evsrs-be_postgres_data 2>/dev/null || true

echo -e "${YELLOW}🌐 Step 5: Removing unused networks...${NC}"
docker network prune -f 2>/dev/null || true

echo -e "${YELLOW}🧽 Step 6: General cleanup...${NC}"
docker system prune -af 2>/dev/null || true

echo -e "${YELLOW}🔍 Step 7: Testing DigitalOcean database connection...${NC}"

# Test DigitalOcean connection
POSTGRES_URL="postgresql://doadmin:AVNS_j67WYcfsnOlYoQBIBdM@evsrs-db-do-user-25819034-0.e.db.ondigitalocean.com:25060/defaultdb?sslmode=require"

if docker run --rm postgres:15-alpine psql "$POSTGRES_URL" -c "SELECT 'DigitalOcean DB OK' as status;" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ DigitalOcean database connection successful${NC}"
else
    echo -e "${RED}❌ DigitalOcean database connection failed${NC}"
    echo "Please check your internet connection and database credentials"
    exit 1
fi

echo -e "${YELLOW}📋 Step 8: Verifying no local PostgreSQL processes...${NC}"

# Check if any postgres processes are running
if pgrep -f postgres > /dev/null; then
    echo -e "${YELLOW}⚠️ Found running PostgreSQL processes:${NC}"
    pgrep -f postgres
    echo -e "${YELLOW}You may need to stop these manually${NC}"
else
    echo -e "${GREEN}✅ No local PostgreSQL processes found${NC}"
fi

# Check port 5432
if netstat -tuln | grep :5432 > /dev/null 2>&1; then
    echo -e "${YELLOW}⚠️ Port 5432 is still in use:${NC}"
    netstat -tuln | grep :5432
else
    echo -e "${GREEN}✅ Port 5432 is free${NC}"
fi

echo -e "${YELLOW}🚀 Step 9: Starting clean deployment...${NC}"

# Start only required services (no postgres)
docker-compose -f docker-compose.vps.yml up -d --build

echo -e "${YELLOW}⏳ Waiting for API to start...${NC}"
sleep 30

echo -e "${YELLOW}🔍 Checking API status...${NC}"

# Check API health
API_URL="http://localhost:8080/health"
for i in {1..10}; do
    if curl -f "$API_URL" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ API is healthy and responding${NC}"
        break
    else
        echo "Attempt $i/10: API not ready yet..."
        sleep 5
    fi
    
    if [ $i -eq 10 ]; then
        echo -e "${RED}❌ API failed to start properly${NC}"
        echo -e "${YELLOW}Checking API logs:${NC}"
        docker-compose -f docker-compose.vps.yml logs --tail=20 evsrs-api
        exit 1
    fi
done

echo -e "${GREEN}🎉 Cleanup completed successfully!${NC}"
echo ""
echo -e "${BLUE}📊 Final Status:${NC}"
docker-compose -f docker-compose.vps.yml ps

echo ""
echo -e "${BLUE}🔗 Service URLs:${NC}"
echo "- API: http://localhost:8080"
echo "- API Health: http://localhost:8080/health"
echo "- Swagger: http://localhost:8080/swagger"
echo "- Portainer: https://localhost:9443"
echo "- Nginx Proxy: http://localhost:81"

echo ""
echo -e "${GREEN}✅ API is now using DigitalOcean PostgreSQL exclusively!${NC}"