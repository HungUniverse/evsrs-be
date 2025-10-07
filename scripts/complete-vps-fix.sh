#!/bin/bash

# Complete VPS Fix Script
# Resolves all deployment issues: git conflicts, permissions, Docker build errors

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔧 Complete VPS Fix & Deploy Script${NC}"
echo -e "${YELLOW}This will resolve all deployment issues and deploy fresh${NC}"
echo ""

# Check if we're in the right directory
if [ ! -f "docker-compose.vps.yml" ]; then
    echo -e "${RED}❌ Not in EVSRS project directory${NC}"
    echo "Please run this from /opt/evsrs/evsrs-be"
    exit 1
fi

echo -e "${YELLOW}🛑 Step 1: Stop all services...${NC}"
docker-compose -f docker-compose.vps.yml down 2>/dev/null || true
docker stop $(docker ps -aq) 2>/dev/null || true

echo -e "${YELLOW}🗑️ Step 2: Complete cleanup...${NC}"
# Remove all containers
docker rm -f $(docker ps -aq) 2>/dev/null || true

# Remove problematic volumes and data
sudo rm -rf data/ logs/ 2>/dev/null || true

# Clean docker system
docker system prune -af 2>/dev/null || true
docker volume prune -f 2>/dev/null || true

echo -e "${YELLOW}📂 Step 3: Fix file permissions...${NC}"
# Fix ownership of the entire project
sudo chown -R $USER:$USER . 2>/dev/null || true

# Create necessary directories with correct permissions
mkdir -p data/nginx-proxy-manager data/letsencrypt logs
chmod 755 data/nginx-proxy-manager data/letsencrypt logs

echo -e "${YELLOW}🗃️ Step 4: Fix git repository...${NC}"
# Remove problematic files that cause conflicts
rm -f scripts/backup.sh 2>/dev/null || true

# Stash any local changes
git stash push -m "VPS auto-stash before deploy - $(date)" 2>/dev/null || true

# Force reset to latest main
git fetch origin
git checkout main
git reset --hard origin/main

echo -e "${YELLOW}🔧 Step 5: Fix configuration...${NC}"
# Ensure .env file has correct format
if [ -f ".env" ]; then
    # Fix any problematic email format
    sed -i 's/MAILGUN_FROM_EMAIL=Eco Rent System <ecorentt.system@mg.ecorentt.me>/MAILGUN_FROM_EMAIL="Eco Rent System <ecorentt.system@mg.ecorentt.me>"/g' .env 2>/dev/null || true
fi

# Make all scripts executable
chmod +x scripts/*.sh 2>/dev/null || true

echo -e "${YELLOW}🔍 Step 6: Test database connection...${NC}"
# Load environment variables
if [ -f ".env" ]; then
    source .env 2>/dev/null || echo "Warning: .env file has syntax issues"
fi

# Test DigitalOcean connection using env vars
POSTGRES_URL="postgresql://${POSTGRES_USER}:${POSTGRES_PASSWORD}@${POSTGRES_HOST}:${POSTGRES_PORT}/${POSTGRES_DB}?sslmode=require"

if docker run --rm postgres:15-alpine psql "$POSTGRES_URL" -c "SELECT 'DB OK' as status;" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ DigitalOcean database connection successful${NC}"
else
    echo -e "${RED}❌ DigitalOcean database connection failed${NC}"
    echo "Please check your internet connection and .env file"
    exit 1
fi

echo -e "${YELLOW}🚀 Step 7: Clean build and deploy...${NC}"
# Build with clean context
docker-compose -f docker-compose.vps.yml build --no-cache evsrs-api

# Start services
docker-compose -f docker-compose.vps.yml up -d

echo -e "${YELLOW}⏳ Step 8: Wait for services...${NC}"
sleep 30

echo -e "${YELLOW}🔍 Step 9: Verify deployment...${NC}"

# Check API health
for i in {1..15}; do
    if curl -f http://localhost:8080/health > /dev/null 2>&1; then
        echo -e "${GREEN}✅ API is healthy and responding${NC}"
        break
    else
        echo "Attempt $i/15: API not ready yet..."
        sleep 5
    fi
    
    if [ $i -eq 15 ]; then
        echo -e "${RED}❌ API failed to start${NC}"
        echo -e "${YELLOW}API logs:${NC}"
        docker-compose -f docker-compose.vps.yml logs --tail=30 evsrs-api
        exit 1
    fi
done

echo -e "${GREEN}🎉 VPS Fix & Deploy completed successfully!${NC}"
echo ""
echo -e "${BLUE}📊 Final Status:${NC}"
docker-compose -f docker-compose.vps.yml ps

echo ""
echo -e "${BLUE}🔗 Service URLs:${NC}"
VPS_IP=$(curl -s ifconfig.me 2>/dev/null || hostname -I | awk '{print $1}')
echo "- API: http://${VPS_IP}:8080"
echo "- API Health: http://${VPS_IP}:8080/health"
echo "- Swagger: http://${VPS_IP}:8080/swagger"
echo "- Portainer: https://${VPS_IP}:9443"
echo "- Nginx Proxy: http://${VPS_IP}:81"

echo ""
echo -e "${GREEN}✅ All issues resolved! CI/CD should now work properly.${NC}"