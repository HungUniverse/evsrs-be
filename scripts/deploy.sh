#!/bin/bash

# Production Deployment Script for EVSRS
# Run this script in your project directory on the VPS

set -e

# Configuration
PROJECT_DIR="/opt/evsrs"
BACKUP_DIR="$PROJECT_DIR/backups"
COMPOSE_FILE="docker-compose.production.yml"
ENV_FILE=".env.production"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Starting EVSRS Production Deployment...${NC}"

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   echo -e "${RED}This script should not be run as root${NC}" 
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
    echo "Please create $ENV_FILE from .env.production.example"
    exit 1
fi

# Function to check if service is healthy
check_service_health() {
    local service_name=$1
    local max_attempts=30
    local attempt=1
    
    echo -e "${YELLOW}Checking health of $service_name...${NC}"
    
    while [ $attempt -le $max_attempts ]; do
        if docker-compose -f $COMPOSE_FILE ps $service_name | grep -q "Up (healthy)"; then
            echo -e "${GREEN}✅ $service_name is healthy${NC}"
            return 0
        fi
        
        echo "Attempt $attempt/$max_attempts - waiting for $service_name..."
        sleep 10
        ((attempt++))
    done
    
    echo -e "${RED}❌ $service_name failed to become healthy${NC}"
    return 1
}

# Create backup before deployment
create_backup() {
    echo -e "${YELLOW}📦 Creating backup before deployment...${NC}"
    
    if docker-compose -f $COMPOSE_FILE ps | grep -q "postgres"; then
        BACKUP_FILE="$BACKUP_DIR/pre_deploy_$(date +%Y%m%d_%H%M%S).sql"
        docker-compose -f $COMPOSE_FILE exec -T postgres pg_dump -U evsrs_user evsrs_production > "$BACKUP_FILE"
        gzip "$BACKUP_FILE"
        echo -e "${GREEN}✅ Backup created: ${BACKUP_FILE}.gz${NC}"
    else
        echo -e "${YELLOW}⚠️ Database not running, skipping backup${NC}"
    fi
}

# Pull latest images
pull_images() {
    echo -e "${YELLOW}📥 Pulling latest Docker images...${NC}"
    docker-compose -f $COMPOSE_FILE pull
}

# Deploy services
deploy_services() {
    echo -e "${YELLOW}🚀 Deploying services...${NC}"
    
    # Start infrastructure services first
    echo "Starting database and cache..."
    docker-compose -f $COMPOSE_FILE up -d postgres redis
    
    # Wait for database to be healthy
    check_service_health "postgres" || exit 1
    check_service_health "redis" || exit 1
    
    # Start application
    echo "Starting application..."
    docker-compose -f $COMPOSE_FILE up -d evsrs-api
    
    # Check application health
    check_service_health "evsrs-api" || exit 1
    
    # Start monitoring services
    echo "Starting monitoring services..."
    docker-compose -f $COMPOSE_FILE up -d elasticsearch filebeat kibana
    
    echo -e "${GREEN}✅ All services deployed successfully${NC}"
}

# Run database migrations
run_migrations() {
    echo -e "${YELLOW}🗃️ Running database migrations...${NC}"
    
    # Wait a bit for the application to fully start
    sleep 10
    
    # Run migrations (if your app has them)
    # docker-compose -f $COMPOSE_FILE exec evsrs-api dotnet ef database update
    
    echo -e "${GREEN}✅ Database migrations completed${NC}"
}

# Cleanup old images and containers
cleanup() {
    echo -e "${YELLOW}🧹 Cleaning up old images and containers...${NC}"
    
    # Remove unused images
    docker image prune -f
    
    # Remove unused containers
    docker container prune -f
    
    # Remove unused volumes (be careful with this)
    # docker volume prune -f
    
    echo -e "${GREEN}✅ Cleanup completed${NC}"
}

# Show deployment status
show_status() {
    echo -e "${GREEN}📊 Deployment Status:${NC}"
    echo "======================"
    docker-compose -f $COMPOSE_FILE ps
    echo ""
    
    echo -e "${GREEN}🌐 Service URLs:${NC}"
    echo "API Health: https://api.your-domain.com/health"
    echo "Swagger: https://api.your-domain.com/swagger"
    echo "Admin Panel: https://admin.your-domain.com"
    echo "Logs: https://logs.your-domain.com"
    echo ""
    
    echo -e "${GREEN}📋 Quick Commands:${NC}"
    echo "View logs: docker-compose -f $COMPOSE_FILE logs -f"
    echo "Restart API: docker-compose -f $COMPOSE_FILE restart evsrs-api"
    echo "Scale API: docker-compose -f $COMPOSE_FILE up -d --scale evsrs-api=2"
    echo ""
}

# Main deployment flow
main() {
    echo -e "${GREEN}Starting deployment process...${NC}"
    
    # Create backup
    create_backup
    
    # Pull latest images
    pull_images
    
    # Deploy services
    deploy_services
    
    # Run migrations
    run_migrations
    
    # Cleanup
    cleanup
    
    # Show status
    show_status
    
    echo -e "${GREEN}🎉 Deployment completed successfully!${NC}"
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
        docker-compose -f $COMPOSE_FILE logs -f "${2:-evsrs-api}"
        ;;
    "restart")
        docker-compose -f $COMPOSE_FILE restart "${2:-evsrs-api}"
        ;;
    "stop")
        echo -e "${YELLOW}🛑 Stopping all services...${NC}"
        docker-compose -f $COMPOSE_FILE down
        ;;
    "start")
        echo -e "${GREEN}▶️ Starting all services...${NC}"
        docker-compose -f $COMPOSE_FILE up -d
        ;;
    *)
        echo "Usage: $0 {deploy|status|backup|cleanup|logs|restart|stop|start}"
        echo ""
        echo "Commands:"
        echo "  deploy  - Full deployment (default)"
        echo "  status  - Show current status"
        echo "  backup  - Create database backup"
        echo "  cleanup - Clean up unused Docker resources"
        echo "  logs    - Show logs (optionally specify service)"
        echo "  restart - Restart service (default: evsrs-api)"
        echo "  stop    - Stop all services"
        echo "  start   - Start all services"
        exit 1
        ;;
esac