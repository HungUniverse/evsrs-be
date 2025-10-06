#!/bin/bash

# Database Migration Script for Production
# Runs Entity Framework migrations on DigitalOcean database

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🗄️ Running Database Migration for Production...${NC}"

# Load environment variables
if [ -f ".env" ]; then
    source .env 2>/dev/null || echo "Warning: .env file has syntax issues"
fi

# Use environment variables with fallbacks
DB_HOST=${POSTGRES_HOST:-"localhost"}
DB_PORT=${POSTGRES_PORT:-"5432"}
DB_NAME=${POSTGRES_DB:-"defaultdb"}
DB_USER=${POSTGRES_USER:-"postgres"}
DB_PASSWORD=${POSTGRES_PASSWORD:-""}
SSL_MODE=${POSTGRES_SSL_MODE:-"Disable"}

# Build connection string from environment variables
DO_CONNECTION_STRING="Host=${DB_HOST}:${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};Pooling=true;MinPoolSize=5;MaxPoolSize=20;Timeout=15;Encoding=UTF8;SslMode=${SSL_MODE};"

echo -e "${YELLOW}📊 Testing database connection to ${DB_HOST}:${DB_PORT}...${NC}"

# Test connection using Docker
POSTGRES_URL="postgresql://${DB_USER}:${DB_PASSWORD}@${DB_HOST}:${DB_PORT}/${DB_NAME}"
if [ "${SSL_MODE}" = "Require" ]; then
    POSTGRES_URL="${POSTGRES_URL}?sslmode=require"
fi

if docker run --rm postgres:15-alpine psql \
  "$POSTGRES_URL" \
  -c "SELECT version();" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Database connection successful${NC}"
else
    echo -e "${RED}❌ Database connection failed${NC}"
    exit 1
fi

echo -e "${YELLOW}🔍 Checking current database schema...${NC}"

# Check if tables exist
TABLES=$(docker run --rm postgres:15-alpine psql \
  "$POSTGRES_URL" \
  -t -c "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public';" 2>/dev/null | tr -d ' ')

echo "Found $TABLES tables in database"

if [ "$TABLES" -eq "0" ] || [ -z "$TABLES" ]; then
    echo -e "${YELLOW}🚀 Database is empty, running initial migration...${NC}"
    
    # Run migrations using the API container
    echo -e "${YELLOW}📦 Building and running migrations...${NC}"
    
    # Create temporary migration container
    docker run --rm \
        -v $(pwd):/source \
        -w /source \
        -e ConnectionStrings__DefaultConnection="$DO_CONNECTION_STRING" \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        bash -c "
            cd EVSRS.API
            dotnet tool install --global dotnet-ef
            export PATH=\"\$PATH:/root/.dotnet/tools\"
            dotnet ef database update --verbose
        "
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Database migration completed successfully${NC}"
    else
        echo -e "${RED}❌ Database migration failed${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✅ Database already contains tables${NC}"
    
    # Check for User table specifically
    USER_EXISTS=$(docker run --rm postgres:15-alpine psql \
      "$POSTGRES_URL" \
      -t -c "SELECT count(*) FROM information_schema.tables WHERE table_name = 'User';" 2>/dev/null | tr -d ' ')
    
    if [ "$USER_EXISTS" -eq "1" ]; then
        echo -e "${GREEN}✅ User table exists${NC}"
    else
        echo -e "${YELLOW}⚠️ User table missing, running migration...${NC}"
        
        docker run --rm \
            -v $(pwd):/source \
            -w /source \
            -e ConnectionStrings__DefaultConnection="$DO_CONNECTION_STRING" \
            mcr.microsoft.com/dotnet/sdk:8.0 \
            bash -c "
                cd EVSRS.API
                dotnet tool install --global dotnet-ef
                export PATH=\"\$PATH:/root/.dotnet/tools\"
                dotnet ef database update --verbose
            "
    fi
fi

echo -e "${GREEN}🎉 Database setup completed!${NC}"

# Show final status
echo -e "${BLUE}📊 Final Database Status:${NC}"
docker run --rm postgres:15-alpine psql \
  "$POSTGRES_URL" \
  -c "\dt"