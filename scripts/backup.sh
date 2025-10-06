#!/bin/bash

# EVSRS VPS Backup Script
# Creates backup of database and important files before deployment

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

BACKUP_DIR="/opt/evsrs/backups"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
COMPOSE_FILE="docker-compose.vps.yml"

echo -e "${BLUE}🗄️ Starting EVSRS Backup...${NC}"

# Create backup directory
mkdir -p $BACKUP_DIR

# Function to backup database
backup_database() {
    echo -e "${YELLOW}📊 Backing up PostgreSQL database...${NC}"
    
    # Get database connection info from environment
    if [ -f ".env" ]; then
        source .env
    fi
    
    BACKUP_FILE="$BACKUP_DIR/postgres_backup_$TIMESTAMP.sql"
    
    # Create database backup using docker exec
    if docker compose -f $COMPOSE_FILE exec -T postgres pg_dump -U $POSTGRES_USER $POSTGRES_DB > $BACKUP_FILE 2>/dev/null; then
        echo -e "${GREEN}✅ Database backup created: $BACKUP_FILE${NC}"
        
        # Compress the backup
        gzip $BACKUP_FILE
        echo -e "${GREEN}✅ Database backup compressed: $BACKUP_FILE.gz${NC}"
    else
        echo -e "${RED}❌ Database backup failed${NC}"
        return 1
    fi
}

# Function to backup important files
backup_files() {
    echo -e "${YELLOW}📁 Backing up important files...${NC}"
    
    FILES_BACKUP="$BACKUP_DIR/files_backup_$TIMESTAMP.tar.gz"
    
    # Backup configuration files and logs
    tar -czf $FILES_BACKUP \
        --exclude='node_modules' \
        --exclude='bin' \
        --exclude='obj' \
        --exclude='.git' \
        .env docker-compose.vps.yml logs/ data/ 2>/dev/null || true
    
    if [ -f "$FILES_BACKUP" ]; then
        echo -e "${GREEN}✅ Files backup created: $FILES_BACKUP${NC}"
    else
        echo -e "${RED}❌ Files backup failed${NC}"
        return 1
    fi
}

# Function to cleanup old backups
cleanup_old_backups() {
    echo -e "${YELLOW}🧹 Cleaning up old backups...${NC}"
    
    # Keep last 7 days of backups
    find $BACKUP_DIR -name "*.sql.gz" -mtime +7 -delete
    find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete
    
    echo -e "${GREEN}✅ Old backups cleaned up${NC}"
}

# Function to show backup status
show_backup_status() {
    echo -e "${GREEN}📊 Backup Status:${NC}"
    echo "==================="
    echo "Backup Directory: $BACKUP_DIR"
    echo "Timestamp: $TIMESTAMP"
    echo ""
    echo "📁 Available Backups:"
    ls -lah $BACKUP_DIR/ 2>/dev/null || echo "No backups found"
    echo ""
    
    # Show disk usage
    echo "💾 Backup Directory Size:"
    du -sh $BACKUP_DIR 2>/dev/null || echo "Unable to calculate size"
}

# Main execution
main() {
    # Check if docker compose is running
    if ! docker compose -f $COMPOSE_FILE ps | grep -q "postgres"; then
        echo -e "${RED}❌ PostgreSQL container not running. Skipping database backup.${NC}"
    else
        backup_database
    fi
    
    backup_files
    cleanup_old_backups
    show_backup_status
    
    echo -e "${GREEN}🎉 Backup completed successfully!${NC}"
}

# Run main function
main "$@"