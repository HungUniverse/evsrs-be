#!/bin/bash

# Fix Git Conflicts on VPS
# This script resolves git conflicts that prevent automated deployment

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔧 Fixing Git Conflicts on VPS${NC}"
echo ""

# Check if we're in the right directory
if [ ! -f "docker-compose.vps.yml" ]; then
    echo -e "${RED}❌ Not in EVSRS project directory${NC}"
    echo "Please run this from /opt/evsrs/evsrs-be"
    exit 1
fi

echo -e "${YELLOW}📊 Current git status:${NC}"
git status

echo ""
echo -e "${YELLOW}🗃️ Stashing local changes...${NC}"

# Stash any local changes
git stash push -m "VPS local changes - $(date)" || echo "Nothing to stash"

echo -e "${YELLOW}📥 Pulling latest code from main...${NC}"

# Force pull from main
git fetch origin
git checkout main
git reset --hard origin/main

echo -e "${GREEN}✅ Git conflicts resolved!${NC}"

echo ""
echo -e "${YELLOW}📋 Git status after fix:${NC}"
git status
git log --oneline -3

echo ""
echo -e "${YELLOW}🔍 Checking if deployment scripts are executable...${NC}"

# Make sure all scripts are executable
chmod +x scripts/*.sh 2>/dev/null || true

echo -e "${GREEN}✅ All scripts are now executable${NC}"

echo ""
echo -e "${BLUE}🚀 Ready for deployment!${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. CI/CD should now work automatically when you push to main branch"
echo "2. Or run manual deployment: ./scripts/deploy-vps.sh"
echo "3. Check API status: curl http://localhost:8080/health"

echo ""
echo -e "${GREEN}🎉 VPS Git conflicts fixed successfully!${NC}"