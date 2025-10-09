#!/bin/bash

# Force Rebuild and Deploy Script
# Forces a complete rebuild with latest code and new controllers

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check for docker-compose or docker compose
COMPOSE_CMD=""
if command -v docker-compose &> /dev/null; then
    COMPOSE_CMD="docker-compose"
elif command -v docker &> /dev/null && docker compose version &> /dev/null; then
    COMPOSE_CMD="docker compose"
else
    echo -e "${RED}❌ Neither 'docker-compose' nor 'docker compose' found${NC}"
    echo "Please install Docker Compose:"
    echo "sudo curl -L \"https://github.com/docker/compose/releases/latest/download/docker-compose-\$(uname -s)-\$(uname -m)\" -o /usr/local/bin/docker-compose"
    echo "sudo chmod +x /usr/local/bin/docker-compose"
    exit 1
fi

echo -e "${BLUE}🔄 Force Rebuild & Deploy with Latest Code${NC}"
echo -e "${YELLOW}Using: ${COMPOSE_CMD}${NC}"
echo -e "${YELLOW}This will rebuild API container with all new controllers${NC}"
echo ""

# Check if we're in the right directory
if [ ! -f "docker-compose.vps.yml" ]; then
    echo -e "${RED}❌ Not in EVSRS project directory${NC}"
    echo "Please run this from the project root directory"
    exit 1
fi

echo -e "${YELLOW}🛑 Step 1: Stop API container...${NC}"
${COMPOSE_CMD} -f docker-compose.vps.yml stop evsrs-api 2>/dev/null || true
docker rm evsrs-api 2>/dev/null || true

echo -e "${YELLOW}🗑️ Step 2: Remove old images...${NC}"
# Remove old API images to force fresh build
docker images | grep evsrs-be | grep evsrs-api | awk '{print $3}' | xargs docker rmi -f 2>/dev/null || true

echo -e "${YELLOW}🔧 Step 3: Clean Docker build cache...${NC}"
docker builder prune -f 2>/dev/null || true

echo -e "${YELLOW}🏗️ Step 4: Force rebuild API with no cache...${NC}"
${COMPOSE_CMD} -f docker-compose.vps.yml build --no-cache --pull evsrs-api

echo -e "${YELLOW}🚀 Step 5: Start API container...${NC}"
${COMPOSE_CMD} -f docker-compose.vps.yml up -d evsrs-api

echo -e "${YELLOW}⏳ Step 6: Wait for API to start...${NC}"
sleep 15

echo -e "${YELLOW}🔍 Step 7: Check API health and endpoints...${NC}"

# Check API health
for i in {1..20}; do
    if curl -f http://localhost:8080/health > /dev/null 2>&1; then
        echo -e "${GREEN}✅ API is healthy and responding${NC}"
        break
    else
        echo "Attempt $i/20: API not ready yet..."
        sleep 3
    fi
    
    if [ $i -eq 20 ]; then
        echo -e "${RED}❌ API failed to start properly${NC}"
        echo -e "${YELLOW}API logs:${NC}"
        docker-compose -f docker-compose.vps.yml logs --tail=30 evsrs-api
        exit 1
    fi
done

echo -e "${YELLOW}📋 Step 8: Verify new controllers...${NC}"

# Check Swagger endpoint
if curl -f http://localhost:8080/swagger > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Swagger endpoint accessible${NC}"
    
    # Get list of available endpoints
    echo -e "${BLUE}📊 Available API endpoints:${NC}"
    curl -s http://localhost:8080/swagger/v1/swagger.json | grep -o '"[^"]*":{"' | grep -v 'definitions\|components' | sort | head -20
    
else
    echo -e "${RED}❌ Swagger endpoint not accessible${NC}"
fi

echo -e "${YELLOW}📊 Final container status:${NC}"
${COMPOSE_CMD} -f docker-compose.vps.yml ps evsrs-api

echo ""
echo -e "${GREEN}🎉 Force rebuild completed!${NC}"
echo ""
echo -e "${BLUE}🔗 Test your new controllers:${NC}"
VPS_IP=$(curl -s ifconfig.me 2>/dev/null || hostname -I | awk '{print $1}')
echo "- API Health: http://${VPS_IP}:8080/health"
echo "- Swagger UI: http://${VPS_IP}:8080/swagger"
echo "- API Base: http://${VPS_IP}:8080/api"
echo ""
echo -e "${YELLOW}💡 To see full API logs: ${COMPOSE_CMD} -f docker-compose.vps.yml logs -f evsrs-api${NC}"