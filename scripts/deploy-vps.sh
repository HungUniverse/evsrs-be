#!/bin/bash

# EVSRS Deployment Script for VPS
# Simple deployment using Docker Compose and Portainer management

set -e

# Configuration
PROJECT_DIR="/opt/evsrs"
COMPOSE_FILE="docker-compose.vps.yml"
ENV_FILE=".env"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Starting EVSRS VPS Deployment...${NC}"

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   echo -e "${RED}This script should not be run as root${NC}" 
   echo "Please run as a regular user with docker permissions"
   exit 1
fi

# Verify we're in the right directory
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}Error: $COMPOSE_FILE not found in current directory${NC}"
    echo "Please run this script from the EVSRS project root directory."
    exit 1
fi

# Check if environment file exists
if [ ! -f "$ENV_FILE" ]; then
    echo -e "${RED}Error: $ENV_FILE not found${NC}"
    echo "Please create $ENV_FILE from .env.vps.example"
    echo "Run: cp .env.vps.example .env"
    exit 1
fi

# Function to check if service is healthy
check_service_health() {
    local service_name=$1
    local max_attempts=30
    local attempt=1
    
    echo -e "${YELLOW}Checking health of $service_name...${NC}"
    
    while [ $attempt -le $max_attempts ]; do
        if docker compose -f $COMPOSE_FILE ps $service_name | grep -q "healthy"; then
            echo -e "${GREEN}✅ $service_name is healthy${NC}"
            return 0
        fi
        
        echo "Attempt $attempt/$max_attempts - waiting for $service_name..."
        sleep 10
        ((attempt++))
    done
    
    echo -e "${RED}❌ $service_name failed to become healthy${NC}"
    echo "Check logs with: docker compose -f $COMPOSE_FILE logs $service_name"
    return 1
}

# Pull latest images
pull_images() {
    echo -e "${YELLOW}📥 Pulling latest Docker images...${NC}"
    docker compose -f $COMPOSE_FILE pull
}

# Deploy core services
deploy_core_services() {
    echo -e "${YELLOW}🚀 Deploying core services...${NC}"
    
    # Start infrastructure services first (External DigitalOcean Database)
    echo "Using external DigitalOcean PostgreSQL database..."
    
    # Test database connection
    echo "Testing database connection..."
    if docker run --rm postgres:15-alpine psql \
      "postgresql://doadmin:AVNS_j67WYcfsnOlYoQBIBdM@evsrs-db-do-user-25819034-0.e.db.ondigitalocean.com:25060/defaultdb?sslmode=require" \
      -c "SELECT 'Connection OK' as status;" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ External database connection successful${NC}"
    else
        echo -e "${RED}❌ External database connection failed${NC}"
        exit 1
    fi
    
    # Start proxy and management services
    echo "Starting proxy and container management..."
    
    # Clean up existing portainer container if exists (but keep volume)
    if docker ps -a --format "table {{.Names}}" | grep -q "^portainer$"; then
        echo "Stopping existing Portainer container..."
        docker stop portainer 2>/dev/null || true
        docker rm portainer 2>/dev/null || true
        echo "Waiting for cleanup..."
        sleep 3
    fi
    
    # Start services with docker-compose
    docker compose -f $COMPOSE_FILE up -d nginx-proxy-manager
    check_service_health "nginx-proxy-manager" || exit 1
    
    # Start Portainer separately to avoid conflicts
    echo "Starting Portainer..."
    docker compose -f $COMPOSE_FILE up -d portainer
    
    # Wait a moment for Portainer to initialize
    sleep 5
    
    # Start application (No database migration needed - using external DigitalOcean DB)
    echo "Starting EVSRS API..."
    docker compose -f $COMPOSE_FILE up -d evsrs-api
    check_service_health "evsrs-api" || exit 1
    
    echo -e "${GREEN}✅ Core services deployed successfully${NC}"
}

# Deploy monitoring services (optional)
deploy_monitoring() {
    echo -e "${YELLOW}📊 Deploying monitoring services (optional)...${NC}"
    read -p "Do you want to deploy monitoring stack (Elasticsearch, Kibana)? [y/N]: " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker compose -f $COMPOSE_FILE --profile monitoring up -d
        echo -e "${GREEN}✅ Monitoring services deployed${NC}"
    else
        echo -e "${BLUE}ℹ️ Skipping monitoring services${NC}"
    fi
}

# Show deployment status
show_status() {
    echo -e "${GREEN}📊 Deployment Status:${NC}"
    echo "======================"
    docker compose -f $COMPOSE_FILE ps
    echo ""
    
    # Get VPS IP
    VPS_IP=$(curl -s ifconfig.me || hostname -I | awk '{print $1}')
    
    echo -e "${GREEN}🌐 Service Access URLs:${NC}"
    echo "- API: http://$VPS_IP:8080"
    echo "- API Health: http://$VPS_IP:8080/health"
    echo "- API Swagger: http://$VPS_IP:8080/swagger"
    echo "- Nginx Proxy Manager: http://$VPS_IP:81"
    echo "- Portainer: https://$VPS_IP:9443"
    
    if docker compose -f $COMPOSE_FILE --profile monitoring ps | grep -q "kibana"; then
        echo "- Kibana (Logs): http://$VPS_IP:5601"
    fi
    
    echo ""
    echo -e "${GREEN}📋 Management Commands:${NC}"
    echo "View logs: docker compose -f $COMPOSE_FILE logs -f [service_name]"
    echo "Restart service: docker compose -f $COMPOSE_FILE restart [service_name]"
    echo "Stop all: docker compose -f $COMPOSE_FILE down"
    echo "Start all: docker compose -f $COMPOSE_FILE up -d"
    echo ""
    
    echo -e "${BLUE}🎯 Next Steps for Cloudflare + Domain Setup:${NC}"
    echo "1. Go to Nginx Proxy Manager: http://$VPS_IP:81"
    echo "2. Default login: admin@example.com / changeme"
    echo "3. Add proxy host pointing to: evsrs-api:8080"
    echo "4. Configure SSL certificate"
    echo "5. Set up Cloudflare DNS to point to this VPS IP: $VPS_IP"
    echo ""
}

# Create backup from DigitalOcean database
create_backup() {
    echo -e "${YELLOW}📦 Creating DigitalOcean database backup...${NC}"
    
    BACKUP_FILE="./backups/backup_$(date +%Y%m%d_%H%M%S).sql"
    mkdir -p ./backups
    
    # Backup from DigitalOcean database directly
    docker run --rm postgres:15-alpine pg_dump \
      "postgresql://doadmin:AVNS_j67WYcfsnOlYoQBIBdM@evsrs-db-do-user-25819034-0.e.db.ondigitalocean.com:25060/defaultdb?sslmode=require" \
      > "$BACKUP_FILE"
    
    gzip "$BACKUP_FILE"
    echo -e "${GREEN}✅ Backup created: ${BACKUP_FILE}.gz${NC}"
}

# Cleanup old images and containers
cleanup() {
    echo -e "${YELLOW}🧹 Cleaning up old images and containers...${NC}"
    
    # Remove unused images
    docker image prune -f
    
    # Remove unused containers
    docker container prune -f
    
    echo -e "${GREEN}✅ Cleanup completed${NC}"
}

# Main deployment flow
main() {
    echo -e "${GREEN}Starting VPS deployment process...${NC}"
    
    # Pull latest images
    pull_images
    
    # Deploy core services
    deploy_core_services
    
    # Deploy monitoring (optional)
    deploy_monitoring
    
    # Cleanup
    cleanup
    
    # Show status
    show_status
    
    echo -e "${GREEN}🎉 VPS Deployment completed successfully!${NC}"
    echo -e "${BLUE}📚 Check DEPLOYMENT-VPS.md for detailed configuration guide${NC}"
}

# Handle script arguments
case "${1:-deploy}" in
    "deploy")
        main
        ;;
    "status")
        show_status
        ;;
    "backup")
        create_backup
        ;;
    "cleanup")
        cleanup
        ;;
    "logs")
        docker compose -f $COMPOSE_FILE logs -f "${2:-evsrs-api}"
        ;;
    "restart")
        docker compose -f $COMPOSE_FILE restart "${2:-evsrs-api}"
        ;;
    "stop")
        echo -e "${YELLOW}🛑 Stopping all services...${NC}"
        docker compose -f $COMPOSE_FILE down
        ;;
    "start")
        echo -e "${GREEN}▶️ Starting all services...${NC}"
        docker compose -f $COMPOSE_FILE up -d
        ;;
    "monitoring")
        echo -e "${YELLOW}📊 Starting monitoring services...${NC}"
        docker compose -f $COMPOSE_FILE --profile monitoring up -d
        ;;
    *)
        echo "Usage: $0 {deploy|status|backup|cleanup|logs|restart|stop|start|monitoring}"
        echo ""
        echo "Commands:"
        echo "  deploy     - Full deployment (default)"
        echo "  status     - Show current status and access URLs"
        echo "  backup     - Create database backup"
        echo "  cleanup    - Clean up unused Docker resources"
        echo "  logs       - Show logs (optionally specify service)"
        echo "  restart    - Restart service (default: evsrs-api)"
        echo "  stop       - Stop all services"
        echo "  start      - Start all services"
        echo "  monitoring - Start monitoring services"
        exit 1
        ;;
esac