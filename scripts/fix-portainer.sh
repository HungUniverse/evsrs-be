#!/bin/bash

# Fix Portainer Script
# Recreates Portainer container with proper configuration

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔧 Fixing Portainer Installation...${NC}"

# Stop and remove existing Portainer
echo -e "${YELLOW}📦 Stopping existing Portainer...${NC}"
docker stop portainer 2>/dev/null || echo "Portainer not running"
docker rm portainer 2>/dev/null || echo "Portainer container not found"

# Check if volume exists and is corrupted
echo -e "${YELLOW}🔍 Checking Portainer data volume...${NC}"
if docker volume inspect portainer_data >/dev/null 2>&1; then
    echo "Volume portainer_data exists"
    
    # Ask if user wants to reset data
    echo -e "${YELLOW}⚠️  Do you want to reset Portainer data? (y/N):${NC}"
    read -r response
    if [[ "$response" =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}🗑️ Removing corrupted volume...${NC}"
        docker volume rm portainer_data || true
    fi
else
    echo "Creating new volume"
fi

# Create Portainer with proper configuration
echo -e "${YELLOW}🚀 Starting new Portainer instance...${NC}"
docker run -d \
  --name portainer \
  --restart unless-stopped \
  -p 9443:9443 \
  -p 8000:8000 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v portainer_data:/data \
  portainer/portainer-ce:latest

# Wait for Portainer to start
echo -e "${YELLOW}⏳ Waiting for Portainer to initialize...${NC}"
sleep 10

# Check if Portainer is running
if docker ps | grep -q portainer; then
    echo -e "${GREEN}✅ Portainer is running successfully!${NC}"
    echo ""
    echo -e "${GREEN}🌐 Access Portainer at:${NC}"
    VPS_IP=$(curl -s ifconfig.me || hostname -I | awk '{print $1}')
    echo "- HTTPS: https://$VPS_IP:9443"
    echo "- You will need to create an admin user on first visit"
    echo ""
    echo -e "${BLUE}📋 Container Status:${NC}"
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep portainer
else
    echo -e "${RED}❌ Portainer failed to start!${NC}"
    echo "Check logs with: docker logs portainer"
    exit 1
fi

echo -e "${GREEN}🎉 Portainer fix completed!${NC}"